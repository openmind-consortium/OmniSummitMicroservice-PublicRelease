// <copyright file="SummitService.cs" company="OpenMind">
// Copyright (c) OpenMind. All rights reserved.
// </copyright>

namespace OpenMindServer
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;
    using Google.Protobuf;
    using Grpc.Core;
    using Medtronic.NeuroStim.Olympus.DataTypes.Measurement;
    using Medtronic.NeuroStim.Olympus.DataTypes.Sensing;
    using Medtronic.SummitAPI.Classes;
    using Medtronic.TelemetryM;
    using Medtronic.TelemetryM.CtmProtocol.Commands;
    using OpenMind;
    using OpenMindServer.Wrappers;

    /// <summary>
    /// Service layer for handling Summit domain objects.
    /// </summary>
    public class SummitService
    {
        /// <summary>
        /// Persistance layer for bridge/device addresses and connections.
        /// </summary>
        private readonly Repository repository;

        /// <summary>
        /// Interface to communicate with the Summit API.
        /// </summary>
        private readonly ISummitManager omniManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SummitService"/> class.
        /// </summary>
        /// <param name="repository">Persistance layer for Summit connections.</param>
        /// <param name="omniManager">SummitManager to use when interacting with the Summit hardware.</param>
        public SummitService(Repository repository, ISummitManager omniManager)
        {
            this.repository = repository;
            this.omniManager = omniManager;
        }

        /// <summary>
        /// Clean up any existing summit resources.
        /// </summary>
        public void Shutdown()
        {
            this.repository.Clear();
            this.omniManager.Dispose();
        }

        /// <summary>
        /// Lists known bridges.
        /// </summary>
        /// <param name="request">The request query used to filter results.</param>
        /// <returns>The query response.</returns>
        public QueryBridgesResponse ListBridges(QueryBridgesRequest request)
        {
            var unionBridges = new List<OpenMind.Bridge>();
            unionBridges.AddRange(this.QueryBridges(request, this.omniManager.GetKnownTelemetry()));
            unionBridges.AddRange(this.QueryBridges(request, this.omniManager.GetUsbTelemetry()));

            var response = new QueryBridgesResponse();
            response.Bridges.AddRange(unionBridges.Distinct().ToList());
            return response;
        }

        /// <summary>
        /// Lists already connected bridges.
        /// </summary>
        /// <param name="request">The request query used to filter results.</param>
        /// <returns>The query response.</returns>
        public QueryBridgesResponse ConnectedBridges(QueryBridgesRequest request)
        {
            var response = new QueryBridgesResponse();
            response.Bridges.AddRange(this.QueryBridges(request, this.omniManager.GetConnectedTelemetry()));
            return response;
        }

        /// <summary>
        /// Connect to bridge.
        /// </summary>
        /// <param name="request">Bridge connection parameters.</param>
        /// <returns>Bridge connection status.</returns>
        public ConnectBridgeResponse ConnectToBridge(ConnectBridgeRequest request)
        {
            var (bridgeName, _) = URINameHelpers.ParseName(request.Name);
            var connection = this.repository.GetConnectionByName(bridgeName);

            if (connection != null)
            {
                // If the name is already used check if the SummitSystem is disposed
                if (connection.Summit.IsDisposed)
                {
                    // If the SummitSystem is disposed, remove it
                    this.repository.RemoveConnectionByName(bridgeName);

                    // Do not exit method and continue on to adding the connection
                }
                else
                {
                    // If the summit system is not disposed, return success because there already is a connection
                    return new ConnectBridgeResponse
                    {
                        Name = request.Name,
                        ConnectionStatus = SummitConnectBridgeStatus.ConnectBridgeSuccess,
                    };
                }
            }

            // If the name has not yet been used, store the connection without error
            var address = this.repository.GetBridgeAddressByName(bridgeName);
            var physicalLayer = InstrumentPhysicalLayers.Any;
            ushort telemetryMode = 3;
            byte telemetryRatio = (byte)request.TelemetryRatio;
            var beepConfig = (CtmBeepEnables)request.BeepEnables;

            if (request.TelemetryMode != null && request.TelemetryMode == "4")
            {
                telemetryMode = 4;
            }

            var summitConnectionStatus = this.omniManager.CreateSummit(out var summitSystem, address, physicalLayer, telemetryMode, telemetryRatio, beepConfig);
            var connectionStatus = (SummitConnectBridgeStatus)summitConnectionStatus;

            if (connectionStatus == SummitConnectBridgeStatus.ConnectBridgeSuccess)
            {
                this.repository.CacheConnection(bridgeName, summitSystem, this.omniManager, request, address);
            }

            var response = new ConnectBridgeResponse
            {
                Name = request.Name,
                ConnectionStatus = connectionStatus,
            };

            return response;
        }

        /// <summary>
        /// Creates a stream for connection status updates to be sent to the client application.
        /// Behavior determined by request.EnableStream arguments:
        ///  - Will only enable streaming on a cached connection if streaming is not already enabled for that connection.
        ///  - Will only disable streaming on a cached connection if streaming is already enabled.
        ///  - All other cases will return immediately to user
        ///  TODO: Error codes.
        /// </summary>
        /// <param name="request">SetStreamEnable request provides both the SummitSystem URI and the desired streaming state.</param>
        /// <param name="responseStream">Stream object provided by the client application, written to by this service until disabled.</param>
        /// <returns>Empty, data returned via responseStream object.</returns>
        public async Task<Google.Protobuf.WellKnownTypes.Empty> CreateBridgeConnectionStream(SetStreamEnable request, IServerStreamWriter<ConnectionUpdate> responseStream)
        {
            // Ensure that the SummitSystem exists in the cache
            SummitServiceInfo theCachedConnection = this.repository.GetConnectionByName(URINameHelpers.ParseName(request.Name).BridgeName);
            if (theCachedConnection != null)
            {
                if (request.EnableStream && theCachedConnection.ConnectionStatusQueue == null)
                {
                    // Create the Blocking Collection that will be used as the message queue for this stream, as well as a buffered update for TryTake purposes
                    theCachedConnection.ConnectionStatusQueue = new BlockingCollection<ConnectionUpdate>();

                    // Push updates to stream as long as the connection message queue isn't marked as being completed
                    // (message queue marked as completed by a SetStreamEnable.EnableStream = false call)
                    while (!theCachedConnection.ConnectionStatusQueue.IsCompleted)
                    {
                        // Wait forever for a new connection status update
                        if (theCachedConnection.ConnectionStatusQueue.TryTake(out var anUpdate, -1))
                        {
                            // Push connection status update out to the response stream
                            try
                            {
                                await responseStream.WriteAsync(anUpdate);
                            }
                            catch
                            {
                                // An error occured while writing the steam
                                // -> this occurs when the stream is preemptively cancelled by the client application or the client application has unexpected closed.
                                // -> We will treat this as an end of streaming event, break out of the loop.
                                break;
                            }
                        }
                    }

                    // Close out the message queue since streaming is no longer occuring
                    theCachedConnection.ConnectionStatusQueue.Dispose();
                    theCachedConnection.ConnectionStatusQueue = null;
                }
                else if (!request.EnableStream && theCachedConnection.ConnectionStatusQueue != null)
                {
                    // Mark the message queue as being complete to shut down streaming
                    theCachedConnection.ConnectionStatusQueue.CompleteAdding();
                }

                // Return remote procedure call as being complete
                return new Google.Protobuf.WellKnownTypes.Empty();
            }
            else
            {
                // Invalid request (no cached connection by that name)
                return new Google.Protobuf.WellKnownTypes.Empty();
            }
        }

        /// <summary>
        /// Creates a stream for Time Domain data updates to be sent to the client application
        /// Behavior determined by request.EnableStream arguments:
        ///  -Will only enable streaming on a cached connection if streaming is not already enabled for that connection.
        ///  -Will only disable streaming on a cached connection if streaming is already enabled.
        ///  -All other cases will return immediately to user.
        /// TODO: Add handling for error codes.
        /// </summary>
        /// <param name="request">SetStreamEnable request provides both the SummitSystem URI and the desired streaming state.</param>
        /// <param name="responseStream">Stream object provided by the client application, written to by this service until disabled.</param>
        /// <returns>Empty, data returned via responseStream object.</returns>
        public async Task<Google.Protobuf.WellKnownTypes.Empty> CreateTimeDomainDataStream(SetDataStreamEnable request, IServerStreamWriter<TimeDomainUpdate> responseStream)
        {
            // Ensure that the SummitSystem exists in the cache
            SummitServiceInfo theCachedConnection = this.repository.GetConnectionByName(URINameHelpers.ParseName(request.Name).BridgeName);
            if (theCachedConnection != null)
            {
                if (request.EnableStream && theCachedConnection.TimeDomainDataQueue == null)
                {
                    // Create the Blocking Collection that will be used as the message queue for this stream, as well as a buffered update for TryTake purposes
                    theCachedConnection.TimeDomainDataQueue = new BlockingCollection<TimeDomainUpdate>();

                    // Push new updates to stream as long as the connection message queue isn't marked as being completed
                    // (message queue marked as completed by a SetStreamEnable.EnableStream = false call)
                    while (!theCachedConnection.TimeDomainDataQueue.IsCompleted)
                    {
                        // Wait forever for a new connection status update
                        if (theCachedConnection.TimeDomainDataQueue.TryTake(out var anUpdate, -1))
                        {
                            // Push connection status update out to the response stream
                            try
                            {
                                await responseStream.WriteAsync(anUpdate);
                            }
                            catch
                            {
                                // An error occured while writing the steam
                                // -> this occurs when the stream is preemptively cancelled by the client application or the client application has unexpected closed.
                                // -> We will treat this as an end of streaming event, break out of the loop.
                                break;
                            }
                        }
                    }

                    // Close out the message queue since streaming is no longer occuring
                    theCachedConnection.TimeDomainDataQueue.Dispose();
                    theCachedConnection.TimeDomainDataQueue = null;
                }
                else if (!request.EnableStream && theCachedConnection.TimeDomainDataQueue != null)
                {
                    // Mark the message queue as being complete to shut down streaming
                    theCachedConnection.TimeDomainDataQueue.CompleteAdding();
                }

                // Return remote procedure call as being complete
                return new Google.Protobuf.WellKnownTypes.Empty();
            }
            else
            {
                // Invalid request (no cached connection by that name)
                return new Google.Protobuf.WellKnownTypes.Empty();
            }
        }

        /// <summary>
        /// Creates a stream for Spectral data updates to be sent to the client application
        /// Behavior determined by request.EnableStream arguments:
        ///  -Will only enable streaming on a cached connection if streaming is not already enabled for that connection.
        ///  -Will only disable streaming on a cached connection if streaming is already enabled.
        ///  -All other cases will return immediately to user.
        /// TODO: Add error code handling.
        /// </summary>
        /// <param name="request">SetStreamEnable request provides both the SummitSystem URI and the desired streaming state.</param>
        /// <param name="responseStream">Stream object provided by the client application, written to by this service until disabled.</param>
        /// <returns>Empty, data returned via responseStream object.</returns>
        public async Task<Google.Protobuf.WellKnownTypes.Empty> CreateFourierDataStream(SetDataStreamEnable request, IServerStreamWriter<FourierTransformUpdate> responseStream)
        {
            // Ensure that the SummitSystem exists in the cache
            SummitServiceInfo theCachedConnection = this.repository.GetConnectionByName(URINameHelpers.ParseName(request.Name).BridgeName);
            if (theCachedConnection != null)
            {
                if (request.EnableStream && theCachedConnection.FourierDataQueue == null)
                {
                    // Create the Blocking Collection that will be used as the message queue for this stream, as well as a buffered update for TryTake purposes
                    theCachedConnection.FourierDataQueue = new BlockingCollection<FourierTransformUpdate>();

                    // Push new connection status updates to stream as long as the connection message queue isn't marked as being completed
                    // (message queue marked as completed by a SetStreamEnable.EnableStream = false call)
                    while (!theCachedConnection.FourierDataQueue.IsCompleted)
                    {
                        // Wait forever for a new connection status update
                        if (theCachedConnection.FourierDataQueue.TryTake(out var anUpdate, -1))
                        {
                            // Push connection status update out to the response stream
                            try
                            {
                                await responseStream.WriteAsync(anUpdate);
                            }
                            catch
                            {
                                // An error occured while writing the steam
                                // -> this occurs when the stream is preemptively cancelled by the client application or the client application has unexpected closed.
                                // -> We will treat this as an end of streaming event, break out of the loop.
                                break;
                            }
                        }
                    }

                    // Close out the message queue since streaming is no longer occuring
                    theCachedConnection.FourierDataQueue.Dispose();
                    theCachedConnection.FourierDataQueue = null;
                }
                else if (!request.EnableStream && theCachedConnection.FourierDataQueue != null)
                {
                    // Mark the message queue as being complete to shut down streaming
                    theCachedConnection.FourierDataQueue.CompleteAdding();
                }

                // Return remote procedure call as being complete
                return new Google.Protobuf.WellKnownTypes.Empty();
            }
            else
            {
                // Invalid request (no cached connection by that name)
                return new Google.Protobuf.WellKnownTypes.Empty();
            }
        }

        /// <summary>
        /// Creates a stream for Band Power data updates to be sent to the client application
        /// Behavior determined by request.EnableStream arguments:
        ///  -Will only enable streaming on a cached connection if streaming is not already enabled for that connection.
        ///  -Will only disable streaming on a cached connection if streaming is already enabled.
        ///  -All other cases will return immediately to user.
        /// TODO: Add error code handling.
        /// </summary>
        /// <param name="request">SetStreamEnable request provides both the SummitSystem URI and the desired streaming state.</param>
        /// <param name="responseStream">Stream object provided by the client application, written to by this service until disabled.</param>
        /// <returns>Empty, data returned via responseStream object.</returns>
        public async Task<Google.Protobuf.WellKnownTypes.Empty> CreateBandPowerDataStream(SetDataStreamEnable request, IServerStreamWriter<BandPowerUpdate> responseStream)
        {
            // Ensure that the SummitSystem exists in the cache
            SummitServiceInfo theCachedConnection = this.repository.GetConnectionByName(URINameHelpers.ParseName(request.Name).BridgeName);
            if (theCachedConnection != null)
            {
                if (request.EnableStream && theCachedConnection.BandPowerDataQueue == null)
                {
                    // Create the Blocking Collection that will be used as the message queue for this stream, as well as a buffered update for TryTake purposes
                    theCachedConnection.BandPowerDataQueue = new BlockingCollection<BandPowerUpdate>();

                    // Push new connection status updates to stream as long as the connection message queue isn't marked as being completed
                    // (message queue marked as completed by a SetStreamEnable.EnableStream = false call)
                    while (!theCachedConnection.BandPowerDataQueue.IsCompleted)
                    {
                        // Wait forever for a new connection status update
                        if (theCachedConnection.BandPowerDataQueue.TryTake(out var anUpdate, -1))
                        {
                            // Push connection status update out to the response stream
                            try
                            {
                                await responseStream.WriteAsync(anUpdate);
                            }
                            catch
                            {
                                // An error occured while writing the steam
                                // -> this occurs when the stream is preemptively cancelled by the client application or the client application has unexpected closed.
                                // -> We will treat this as an end of streaming event, break out of the loop.
                                break;
                            }
                        }
                    }

                    // Close out the message queue since streaming is no longer occuring
                    theCachedConnection.BandPowerDataQueue.Dispose();
                    theCachedConnection.BandPowerDataQueue = null;
                }
                else if (!request.EnableStream && theCachedConnection.BandPowerDataQueue != null)
                {
                    // Mark the message queue as being complete to shut down streaming
                    theCachedConnection.BandPowerDataQueue.CompleteAdding();
                }

                // Return remote procedure call as being complete
                return new Google.Protobuf.WellKnownTypes.Empty();
            }
            else
            {
                // Invalid request (no cached connection by that name)
                return new Google.Protobuf.WellKnownTypes.Empty();
            }
        }

        /// <summary>
        /// Creates a stream for Inertial data updates to be sent to the client application
        ///  -For Summit this is only accelerometer data.
        /// Behavior determined by request.EnableStream arguments:
        ///  -Will only enable streaming on a cached connection if streaming is not already enabled for that connection.
        ///  -Will only disable streaming on a cached connection if streaming is already enabled.
        ///  -All other cases will return immediately to user.
        /// TODO: Add error code handling.
        /// </summary>
        /// <param name="request">SetStreamEnable request provides both the SummitSystem URI and the desired streaming state.</param>
        /// <param name="responseStream">Stream object provided by the client application, written to by this service until disabled.</param>
        /// <returns>Empty, data returned via responseStream object.</returns>
        public async Task<Google.Protobuf.WellKnownTypes.Empty> CreateInertialDataStream(SetDataStreamEnable request, IServerStreamWriter<InertialUpdate> responseStream)
        {
            // Ensure that the SummitSystem exists in the cache
            SummitServiceInfo theCachedConnection = this.repository.GetConnectionByName(URINameHelpers.ParseName(request.Name).BridgeName);
            if (theCachedConnection != null)
            {
                if (request.EnableStream && theCachedConnection.InertialDataQueue == null)
                {
                    // Create the Blocking Collection that will be used as the message queue for this stream, as well as a buffered update for TryTake purposes
                    theCachedConnection.InertialDataQueue = new BlockingCollection<InertialUpdate>();

                    // Push new connection status updates to stream as long as the connection message queue isn't marked as being completed
                    // (message queue marked as completed by a SetStreamEnable.EnableStream = false call)
                    while (!theCachedConnection.InertialDataQueue.IsCompleted)
                    {
                        // Wait forever for a new connection status update
                        if (theCachedConnection.InertialDataQueue.TryTake(out var anUpdate, -1))
                        {
                            // Push connection status update out to the response stream
                            try
                            {
                                await responseStream.WriteAsync(anUpdate);
                            }
                            catch
                            {
                                // An error occured while writing the steam
                                // -> this occurs when the stream is preemptively cancelled by the client application or the client application has unexpected closed.
                                // -> We will treat this as an end of streaming event, break out of the loop.
                                break;
                            }
                        }
                    }

                    // Close out the message queue since streaming is no longer occuring
                    theCachedConnection.InertialDataQueue.Dispose();
                    theCachedConnection.InertialDataQueue = null;
                }
                else if (!request.EnableStream && theCachedConnection.InertialDataQueue != null)
                {
                    // Mark the message queue as being complete to shut down streaming
                    theCachedConnection.InertialDataQueue.CompleteAdding();
                }

                // Return remote procedure call as being complete
                return new Google.Protobuf.WellKnownTypes.Empty();
            }
            else
            {
                // Invalid request (no cached connection by that name)
                return new Google.Protobuf.WellKnownTypes.Empty();
            }
        }

        /// <summary>
        /// Creates a stream for Adaptive Status updates to be sent to the client application
        /// Behavior determined by request.EnableStream arguments:
        ///  -Will only enable streaming on a cached connection if streaming is not already enabled for that connection.
        ///  -Will only disable streaming on a cached connection if streaming is already enabled.
        ///  -All other cases will return immediately to user.
        /// TODO: Add error code handling.
        /// </summary>
        /// <param name="request">SetStreamEnable request provides both the SummitSystem URI and the desired streaming state.</param>
        /// <param name="responseStream">Stream object provided by the client application, written to by this service until disabled.</param>
        /// <returns>Empty, data returned via responseStream object.</returns>
        public async Task<Google.Protobuf.WellKnownTypes.Empty> CreateAdaptiveDataStream(SetDataStreamEnable request, IServerStreamWriter<AdaptiveUpdate> responseStream)
        {
            // Ensure that the SummitSystem exists in the cache
            SummitServiceInfo theCachedConnection = this.repository.GetConnectionByName(URINameHelpers.ParseName(request.Name).BridgeName);
            if (theCachedConnection != null)
            {
                if (request.EnableStream && theCachedConnection.AdaptiveDataQueue == null)
                {
                    // Create the Blocking Collection that will be used as the message queue for this stream, as well as a buffered update for TryTake purposes
                    theCachedConnection.AdaptiveDataQueue = new BlockingCollection<AdaptiveUpdate>();

                    // Push new connection status updates to stream as long as the connection message queue isn't marked as being completed
                    // (message queue marked as completed by a SetStreamEnable.EnableStream = false call)
                    while (!theCachedConnection.AdaptiveDataQueue.IsCompleted)
                    {
                        // Wait forever for a new connection status update
                        if (theCachedConnection.AdaptiveDataQueue.TryTake(out var anUpdate, -1))
                        {
                            // Push connection status update out to the response stream
                            try
                            {
                                await responseStream.WriteAsync(anUpdate);
                            }
                            catch
                            {
                                // An error occured while writing the steam
                                // -> this occurs when the stream is preemptively cancelled by the client application or the client application has unexpected closed.
                                // -> We will treat this as an end of streaming event, break out of the loop.
                                break;
                            }
                        }
                    }

                    // Close out the message queue since streaming is no longer occuring
                    theCachedConnection.AdaptiveDataQueue.Dispose();
                    theCachedConnection.AdaptiveDataQueue = null;
                }
                else if (!request.EnableStream && theCachedConnection.AdaptiveDataQueue != null)
                {
                    // Mark the message queue as being complete to shut down streaming
                    theCachedConnection.AdaptiveDataQueue.CompleteAdding();
                }

                // Return remote procedure call as being complete
                return new Google.Protobuf.WellKnownTypes.Empty();
            }
            else
            {
                // Invalid request (no cached connection by that name)
                return new Google.Protobuf.WellKnownTypes.Empty();
            }
        }

        /// <summary>
        /// Creates a stream for loop recorder status updates to be sent to the client application
        /// Behavior determined by request.EnableStream arguments:
        ///  -Will only enable streaming on a cached connection if streaming is not already enabled for that connection.
        ///  -Will only disable streaming on a cached connection if streaming is already enabled.
        ///  -All other cases will return immediately to user.
        /// TODO: Add error code handling.
        /// </summary>
        /// <param name="request">SetStreamEnable request provides both the SummitSystem URI and the desired streaming state.</param>
        /// <param name="responseStream">Stream object provided by the client application, written to by this service until disabled.</param>
        /// <returns>Empty, data returned via responseStream object.</returns>
        public async Task<Google.Protobuf.WellKnownTypes.Empty> CreateLoopDataStream(SetDataStreamEnable request, IServerStreamWriter<LoopRecordUpdate> responseStream)
        {
            // Ensure that the SummitSystem exists in the cache
            SummitServiceInfo theCachedConnection = this.repository.GetConnectionByName(URINameHelpers.ParseName(request.Name).BridgeName);
            if (theCachedConnection != null)
            {
                if (request.EnableStream && theCachedConnection.LoopRecordQueue == null)
                {
                    // Create the Blocking Collection that will be used as the message queue for this stream, as well as a buffered update for TryTake purposes
                    theCachedConnection.LoopRecordQueue = new BlockingCollection<LoopRecordUpdate>();

                    // Push new connection status updates to stream as long as the connection message queue isn't marked as being completed
                    // (message queue marked as completed by a SetStreamEnable.EnableStream = false call)
                    while (!theCachedConnection.LoopRecordQueue.IsCompleted)
                    {
                        // Wait forever for a new connection status update
                        if (theCachedConnection.LoopRecordQueue.TryTake(out var anUpdate, -1))
                        {
                            // Push connection status update out to the response stream
                            try
                            {
                                await responseStream.WriteAsync(anUpdate);
                            }
                            catch
                            {
                                // An error occured while writing the steam
                                // -> this occurs when the stream is preemptively cancelled by the client application or the client application has unexpected closed.
                                // -> We will treat this as an end of streaming event, break out of the loop.
                                break;
                            }
                        }
                    }

                    // Close out the message queue since streaming is no longer occuring
                    theCachedConnection.LoopRecordQueue.Dispose();
                    theCachedConnection.LoopRecordQueue = null;
                }
                else if (!request.EnableStream && theCachedConnection.LoopRecordQueue != null)
                {
                    // Mark the message queue as being complete to shut down streaming
                    theCachedConnection.LoopRecordQueue.CompleteAdding();
                }

                // Return remote procedure call as being complete
                return new Google.Protobuf.WellKnownTypes.Empty();
            }
            else
            {
                // Invalid request (no cached connection by that name)
                return new Google.Protobuf.WellKnownTypes.Empty();
            }
        }

        /// <summary>
        /// Creates a stream for echo data updates to be sent to the client application
        ///  -This is Summit behavior for inserting a marker into the JSON logs with the INS timestamp.
        /// Behavior determined by request.EnableStream arguments:
        ///  -Will only enable streaming on a cached connection if streaming is not already enabled for that connection.
        ///  -Will only disable streaming on a cached connection if streaming is already enabled.
        ///  -All other cases will return immediately to user.
        /// TODO: Add error code handling.
        /// </summary>
        /// <param name="request">SetStreamEnable request provides both the SummitSystem URI and the desired streaming state.</param>
        /// <param name="responseStream">Stream object provided by the client application, written to by this service until disabled.</param>
        /// <returns>Empty, data returned via responseStream object.</returns>
        public async Task<Google.Protobuf.WellKnownTypes.Empty> CreateEchoDataStream(SetDataStreamEnable request, IServerStreamWriter<EchoUpdate> responseStream)
        {
            // Ensure that the SummitSystem exists in the cache
            SummitServiceInfo theCachedConnection = this.repository.GetConnectionByName(URINameHelpers.ParseName(request.Name).BridgeName);
            if (theCachedConnection != null)
            {
                if (request.EnableStream && theCachedConnection.EchoDataQueue == null)
                {
                    // Create the Blocking Collection that will be used as the message queue for this stream, as well as a buffered update for TryTake purposes
                    theCachedConnection.EchoDataQueue = new BlockingCollection<EchoUpdate>();

                    // Push new connection status updates to stream as long as the connection message queue isn't marked as being completed
                    // (message queue marked as completed by a SetStreamEnable.EnableStream = false call)
                    while (!theCachedConnection.EchoDataQueue.IsCompleted)
                    {
                        // Wait forever for a new connection status update
                        if (theCachedConnection.EchoDataQueue.TryTake(out var anUpdate, -1))
                        {
                            // Push connection status update out to the response stream
                            try
                            {
                                await responseStream.WriteAsync(anUpdate);
                            }
                            catch
                            {
                                // An error occured while writing the steam
                                // -> this occurs when the stream is preemptively cancelled by the client application or the client application has unexpected closed.
                                // -> We will treat this as an end of streaming event, break out of the loop.
                                break;
                            }
                        }
                    }

                    // Close out the message queue since streaming is no longer occuring
                    theCachedConnection.EchoDataQueue.Dispose();
                    theCachedConnection.EchoDataQueue = null;
                }
                else if (!request.EnableStream && theCachedConnection.EchoDataQueue != null)
                {
                    // Mark the message queue as being complete to shut down streaming
                    theCachedConnection.EchoDataQueue.CompleteAdding();
                }

                // Return remote procedure call as being complete
                return new Google.Protobuf.WellKnownTypes.Empty();
            }
            else
            {
                // Invalid request (no cached connection by that name)
                return new Google.Protobuf.WellKnownTypes.Empty();
            }
        }

        /// <summary>
        /// Disconnect from bridge.
        /// </summary>
        /// <param name="request">Bridge to disconnect from.</param>
        /// <returns>Empty gRPC response.</returns>
        public Google.Protobuf.WellKnownTypes.Empty DisconnectFromBridge(DisconnectBridgeRequest request)
        {
            var (bridgeName, _) = URINameHelpers.ParseName(request.Name);
            var bridge = this.repository.GetConnectionByName(bridgeName);

            // Ensure the bridge is not null
            if (bridge != null)
            {
                this.omniManager.DisposeSummit(bridge.Summit);
                this.repository.RemoveConnectionByName(bridgeName);
            }

            return new Google.Protobuf.WellKnownTypes.Empty();
        }

        /// <summary>
        /// Describe bridge parameters.
        /// </summary>
        /// <param name="request">The bridge to query.</param>
        /// <returns>Bridge details.</returns>
        public DescribeBridgeResponse DescribeBridge(DescribeBridgeRequest request)
        {
            var bridge = this.repository.GetConnectionByName(URINameHelpers.ParseName(request.Name).BridgeName);

            // Ensure the bridge is not null
            if (bridge == null)
            {
                return new DescribeBridgeResponse() { Error = new SummitError() { RejectCode = (SummitApiErrorCode)(-1), Message = "Bridge requested is not connected" } };
            }

            var status = bridge.Summit.ReadTelemetryModuleInfo(out var telemetryInfo);

            if (status.RejectCode != (int)APIRejectCodes.NoError)
            {
                return new DescribeBridgeResponse
                {
                    Name = request.Name,
                    Error = this.SummitToGrpcApiStatus(status),
                };
            }

            DescribeBridgeResponse theResponse = new DescribeBridgeResponse();
            theResponse.Name = request.Name;
            theResponse.FirmwareVersion = telemetryInfo.FirmwareVersion;
            theResponse.BatteryLevel = telemetryInfo.BatteryLevel;
            theResponse.TelemetryRatio = telemetryInfo.TelmRatio;
            theResponse.PhysicalLayer = (SummitPhysicalLayer)telemetryInfo.PhysicalLayer;
            theResponse.BeepEnables = (SummitBeepConfig)telemetryInfo.BeepEnables;
            theResponse.Error = this.SummitToGrpcApiStatus(status);

            // String assignment - check for nulls
            if (!string.IsNullOrEmpty(telemetryInfo.ModuleType))
            {
                theResponse.ModuleType = telemetryInfo.ModuleType;
            }
            else
            {
                theResponse.ModuleType = string.Empty;
            }

            if (!string.IsNullOrEmpty(telemetryInfo.WireType))
            {
                theResponse.WireType = telemetryInfo.WireType;
            }
            else
            {
                theResponse.WireType = string.Empty;
            }

            if (!string.IsNullOrEmpty(telemetryInfo.SerialNumber))
            {
                theResponse.SerialNumber = telemetryInfo.SerialNumber;
            }
            else
            {
                theResponse.SerialNumber = string.Empty;
            }

            if (!string.IsNullOrEmpty(telemetryInfo.BatteryStatus))
            {
                theResponse.BatteryStatus = telemetryInfo.BatteryStatus;
            }
            else
            {
                theResponse.BatteryStatus = string.Empty;
            }

            if (!string.IsNullOrEmpty(telemetryInfo.TelmMode))
            {
                theResponse.TelemetryMode = telemetryInfo.TelmMode;
            }
            else
            {
                theResponse.TelemetryMode = string.Empty;
            }

            return theResponse;
        }

        /// <summary>
        /// Lists INS devices.
        /// </summary>
        /// <param name="request">The bridge/device to query for.</param>
        /// <returns>A list of INS devices.</returns>
        public ListDeviceResponse ListDevices(ListDeviceRequest request)
        {
            var response = new ListDeviceResponse();
            var bridge = this.repository.GetConnectionByName(URINameHelpers.ParseName(request.Query).BridgeName);

            // Ensure the bridge is not null
            if (bridge == null)
            {
                return new ListDeviceResponse() { Error = new SummitError() { RejectCode = (SummitApiErrorCode)(-1), Message = "Bridge requested is not connected" } };
            }

            var status = bridge.Summit.OlympusDiscovery(out var discoveredDevices);

            response.Error = this.SummitToGrpcApiStatus(status);

            if (discoveredDevices == null)
            {
                return response;
            }

            foreach (var discoveredDevice in discoveredDevices)
            {
                var name = URINameHelpers.BuildNameFromSerialNumbers(URINameHelpers.GetBridgeSerial(request.Query), discoveredDevice.deviceSerial);
                this.repository.CacheDeviceAddress(name, discoveredDevice);
                response.Devices.Add(new Device
                {
                    Name = name,
                });
            }

            return response;
        }

        /// <summary>
        /// Connects to a device.
        /// </summary>
        /// <param name="request">Device connection parameters.</param>
        /// <returns>Device connection status.</returns>
        public ConnectDeviceResponse ConnectToDevice(ConnectDeviceRequest request)
        {
            var response = new ConnectDeviceResponse();
            var (bridgeName, fullName) = URINameHelpers.ParseName(request.Name);
            var bridge = this.repository.GetConnectionByName(bridgeName);

            // Ensure the bridge is not null
            if (bridge == null)
            {
                return new ConnectDeviceResponse() { Error = new SummitError() { RejectCode = (SummitApiErrorCode)(-1), Message = "Bridge requested is not connected" } };
            }

            // Determine if null connect is requested or if a discovered device should be used
            DiscoveredDevice? deviceAddress = this.repository.GetDeviceAddressByName(fullName);
            if (deviceAddress == null && bridgeName != fullName)
            {
                return new ConnectDeviceResponse() { Error = new SummitError() { RejectCode = (SummitApiErrorCode)(-1), Message = "Requested Device not in Repostiory" } };
            }

            // Proceed with the connect
            var disableAnnotations = true;
            var status = bridge.Summit.StartInsSession(deviceAddress, out ConnectReturn connectReturn, disableAnnotations);
            response.Name = request.Name;
            response.ConnectionStatus = (uint)connectReturn;
            response.Error = this.SummitToGrpcApiStatus(status);

            return response;
        }

        /// <summary>
        /// Disconnects from a device.
        /// </summary>
        /// <param name="request">The device to disconnect from.</param>
        /// <returns>Nothing.</returns>
        public Google.Protobuf.WellKnownTypes.Empty DisconnectFromDevice(DisconnectDeviceRequest request)
        {
            var (bridgeName, fullName) = URINameHelpers.ParseName(request.Name);
            var device = this.repository.GetConnectionByName(bridgeName);
            if (device != null)
            {
                this.omniManager.DisposeSummit(device.Summit);
                this.repository.RemoveConnectionByName(bridgeName);
            }

            return new Google.Protobuf.WellKnownTypes.Empty();
        }

        /// <summary>
        /// Get the device status.
        /// </summary>
        /// <param name="request">Device status request.</param>
        /// <returns>Device status response.</returns>
        public DeviceStatusResponse DeviceStatus(DeviceStatusRequest request)
        {
            var (bridgeName, _) = URINameHelpers.ParseName(request.Name);
            var device = this.repository.GetConnectionByName(bridgeName);

            // Ensure the device is not null
            if (device == null)
            {
                return new DeviceStatusResponse() { Error = new SummitError() { RejectCode = (SummitApiErrorCode)(-1), Message = "Device requested is not connected" } };
            }

            var status = device.Summit.ReadBatteryLevel(out var batteryStatusResult);

            if (batteryStatusResult != null)
            {
                return new DeviceStatusResponse
                {
                    Name = request.Name,
                    BatteryLevelPercent = batteryStatusResult.BatteryLevelPercent,
                    BatteryLevelVoltage = batteryStatusResult.BatteryVoltage,
                    Error = this.SummitToGrpcApiStatus(status),
                    BatterySoc = batteryStatusResult.BatterySOC,
                    SocUncertainty = batteryStatusResult.SOCUncertainty,
                    ManufacturedCapacity = batteryStatusResult.ManufacturedCapacity,
                    EstimatedCapacity = batteryStatusResult.EstimatedCapacity,
                    TherapyUnavailableSoc = batteryStatusResult.TherapyUnavailableSOC,
                    FullSoc = batteryStatusResult.FullSOC,
                };
            }
            else
            {
                return new DeviceStatusResponse
                {
                    Name = request.Name,
                    Error = this.SummitToGrpcApiStatus(status),
                };
            }
        }

        /// <summary>
        /// Configure all sense settings.
        /// </summary>
        /// <param name="request">Sense setting configuration and Name for device path.</param>
        /// <returns>SenseConfigurationResponse including error info.</returns>
        public SenseConfigurationResponse ConfigureSense(SenseConfigurationRequest request)
        {
            var (bridgeName, _) = URINameHelpers.ParseName(request.Name);
            var device = this.repository.GetConnectionByName(bridgeName);

            // Ensure the device is not null
            if (device == null)
            {
                return new SenseConfigurationResponse() { Error = new SummitError() { RejectCode = (SummitApiErrorCode)(-1), Message = "Device requested is not connected" } };
            }

            var timeDomainChannels = new List<TimeDomainChannel>(4);
            var fftSize = (FastFourierTransformSizes)0;
            var fftInterval = 50;
            var fftWindowLoad = (FastFourierTransformWindowAutoLoads)0;
            var fftEnableWindow = false;
            var fftWeightMultiplies = (FastFourierTransformWeightMultiplies)0;
            var fftBinsToStream = 0;
            var fftBinsToStreamOffset = 0;
            var powerChannels = new List<PowerChannel>();
            BandEnables bandEnables = 0;
            var miscBridging = (BridgingConfiguration)0;
            var miscStreamRate = (OpenMind.StreamingFrameRate)5;
            var miscLoopRecordingTriggers = (OpenMind.LoopRecordingTriggers)0;
            var miscLoopRecordingPostBufferTime = 0;
            var accelerometerSampleRate = (AccelerometerSampleRate)0;
            var senseStates = SenseStates.None;
            var senseTimeDomainChannel = SenseTimeDomainChannel.Ch0;

            try
            {
                // Time Domain Channels. Add all 4 channels into Summit API type.
                foreach (var tdChannel in request.TdChannelConfigs)
                {
                    timeDomainChannels.Add(new TimeDomainChannel(
                        tdChannel.Disabled ? TdSampleRates.Disabled : (TdSampleRates)request.TimedomainSamplingRate,
                        (TdMuxInputs)tdChannel.Minus,
                        (TdMuxInputs)tdChannel.Plus,
                        (TdEvokedResponseEnable)tdChannel.EvokedMode,
                        (TdLpfStage1)tdChannel.LowPassFilterStage1,
                        (TdLpfStage2)tdChannel.LowPassFilterStage2,
                        (TdHpfs)tdChannel.HighPassFilters));
                }

                // FFT
                fftSize = request.FftConfig.Size;
                fftInterval = request.FftConfig.Interval;
                fftWindowLoad = request.FftConfig.WindowLoad;
                fftEnableWindow = request.FftConfig.EnableWindow;
                fftWeightMultiplies = request.FftConfig.BandFormationConfig;
                fftBinsToStream = request.FftConfig.BinsToStream;
                fftBinsToStreamOffset = request.FftConfig.BinsToStreamOffset;

                if (request.PowerChannelConfig.PowerBandEnables.Count() != 8)
                {
                    // The PowerBandEnables must be size of 8
                    return new SenseConfigurationResponse
                    {
                        Name = request.Name,
                        Error = new SummitError
                        {
                            RejectCode = (SummitApiErrorCode)(-1),
                            Message = "PowerBandEnables must be size of 8",
                        },
                    };
                }

                if (request.PowerChannelConfig.PowerBandConfiguration.Count() != 8)
                {
                    // The PowerBandConfiguration must be size of 8
                    return new SenseConfigurationResponse
                    {
                        Name = request.Name,
                        Error = new SummitError
                        {
                            RejectCode = (SummitApiErrorCode)(-1),
                            Message = "PowerBandConfiguration must be size of 8",
                        },
                    };
                }

                for (int i = 0; i < request.PowerChannelConfig.PowerBandConfiguration.Count(); i += 2)
                {
                    powerChannels.Add(new PowerChannel(
                        (ushort)request.PowerChannelConfig.PowerBandConfiguration[i].BandStart,
                        (ushort)request.PowerChannelConfig.PowerBandConfiguration[i].BandStop,
                        (ushort)request.PowerChannelConfig.PowerBandConfiguration[i + 1].BandStart,
                        (ushort)request.PowerChannelConfig.PowerBandConfiguration[i + 1].BandStop));
                }

                // If PowerBandEnables is true, then convert bool to int and multiply by enum value. If 1 then it sets it, if 0 then it doesn't. Bitwise or them together.
                bandEnables = (BandEnables)((Convert.ToInt32(request.PowerChannelConfig.PowerBandEnables[0]) * (byte)BandEnables.Ch0Band0Enabled) |
                    (Convert.ToInt32(request.PowerChannelConfig.PowerBandEnables[1]) * (byte)BandEnables.Ch0Band1Enabled) |
                        (Convert.ToInt32(request.PowerChannelConfig.PowerBandEnables[2]) * (byte)BandEnables.Ch1Band0Enabled) |
                        (Convert.ToInt32(request.PowerChannelConfig.PowerBandEnables[3]) * (byte)BandEnables.Ch1Band1Enabled) |
                        (Convert.ToInt32(request.PowerChannelConfig.PowerBandEnables[4]) * (byte)BandEnables.Ch2Band0Enabled) |
                        (Convert.ToInt32(request.PowerChannelConfig.PowerBandEnables[5]) * (byte)BandEnables.Ch2Band1Enabled) |
                        (Convert.ToInt32(request.PowerChannelConfig.PowerBandEnables[6]) * (byte)BandEnables.Ch3Band0Enabled) |
                        (Convert.ToInt32(request.PowerChannelConfig.PowerBandEnables[7]) * (byte)BandEnables.Ch3Band1Enabled));

                // Misc
                miscBridging = request.MiscStreamConfig.Bridging;
                miscStreamRate = request.MiscStreamConfig.StreamingRate;
                miscLoopRecordingTriggers = request.MiscStreamConfig.LoopRecordTriggers;
                miscLoopRecordingPostBufferTime = (int)request.MiscStreamConfig.LoopRecordingPostBufferTime;

                // Acc
                accelerometerSampleRate = request.AccelerometerConfig.SampleRate;

                // Sense States
                senseStates = (SenseStates)((Convert.ToInt32(request.SenseEnablesConfig.EnableTimedomain) * (byte)SenseStates.LfpSense) |
                    (Convert.ToInt32(request.SenseEnablesConfig.EnableFft) * (byte)SenseStates.Fft) |
                    (Convert.ToInt32(request.SenseEnablesConfig.EnablePower) * (byte)SenseStates.Power) |
                    (Convert.ToInt32(request.SenseEnablesConfig.EnableLd0) * (byte)SenseStates.DetectionLd0) |
                    (Convert.ToInt32(request.SenseEnablesConfig.EnableLd1) * (byte)SenseStates.DetectionLd1) |
                    (Convert.ToInt32(request.SenseEnablesConfig.EnableAdaptiveStim) * (byte)SenseStates.AdaptiveStim) |
                    (Convert.ToInt32(request.SenseEnablesConfig.EnableLoopRecording) * (byte)SenseStates.LoopRecording));

                senseTimeDomainChannel = (SenseTimeDomainChannel)request.SenseEnablesConfig.FftStreamChannel;

                // FFT Set Summit API Type
                var fftSettings = new FftConfiguration();
                fftSettings.Size = (FftSizes)fftSize;
                fftSettings.Interval = (ushort)fftInterval;
                fftSettings.WindowEnabled = fftEnableWindow;
                fftSettings.WindowLoad = (FftWindowAutoLoads)fftWindowLoad;
                fftSettings.BandFormationConfig = (FftWeightMultiplies)fftWeightMultiplies;
                fftSettings.StreamSizeBins = (ushort)fftBinsToStream;
                fftSettings.StreamOffsetBins = (ushort)fftBinsToStreamOffset;

                // Misc Summit API Type
                var miscSettings = new MiscellaneousSensing();
                miscSettings.Bridging = (BridgingConfig)miscBridging;
                miscSettings.StreamingRate = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.StreamingFrameRate)miscStreamRate;
                miscSettings.LrTriggers = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.LoopRecordingTriggers)miscLoopRecordingTriggers;
                miscSettings.LrPostBufferTime = (ushort)miscLoopRecordingPostBufferTime;

                // Stop Sensing. Required by Medtronic API prior to configuring sense
                var status = device.Summit.WriteSensingState(SenseStates.None, 0x00);
                if (status.RejectCode != 0)
                {
                    goto SummitApiError;
                }

                // Write Sensing Configuration to Device. Return failure right away if an error occurred.
                status = device.Summit.WriteSensingTimeDomainChannels(timeDomainChannels);
                if (status.RejectCode != 0)
                {
                    goto SummitApiError;
                }

                status = device.Summit.WriteSensingFftSettings(fftSettings);
                if (status.RejectCode != 0)
                {
                    goto SummitApiError;
                }

                status = device.Summit.WriteSensingPowerChannels(bandEnables, powerChannels);
                if (status.RejectCode != 0)
                {
                    goto SummitApiError;
                }

                status = device.Summit.WriteSensingMiscSettings(miscSettings);
                if (status.RejectCode != 0)
                {
                    goto SummitApiError;
                }

                status = device.Summit.WriteSensingAccelSettings((AccelSampleRate)accelerometerSampleRate);
                if (status.RejectCode != 0)
                {
                    goto SummitApiError;
                }

                status = device.Summit.WriteSensingState(senseStates, senseTimeDomainChannel);
                if (status.RejectCode != 0)
                {
                    goto SummitApiError;
                }

                return new SenseConfigurationResponse
                {
                    Name = request.Name,
                    Error = this.SummitToGrpcApiStatus(status),
                };

            SummitApiError:
                return new SenseConfigurationResponse
                {
                    Name = request.Name,
                    Error = this.SummitToGrpcApiStatus(status),
                };
            }
            catch (Exception e)
            {
                return new SenseConfigurationResponse
                {
                    Name = request.Name,
                    Error = new SummitError()
                    {
                        RejectCode = (SummitApiErrorCode)(-1),
                        Message = e.Message,
                    },
                };
            }
        }

        /// <summary>
        /// Lead Integrity test.
        /// </summary>
        /// <param name="request">Request info.</param>
        /// <returns>Impedance values between pairs of leads.</returns>
        public ImpedanceResponse LeadIntegrityTest(ImpedanceRequest request)
        {
            var (bridgeName, fullName) = URINameHelpers.ParseName(request.Name);
            var device = this.repository.GetConnectionByName(bridgeName);

            // Ensure the device is not null
            if (device == null)
            {
                return new ImpedanceResponse() { Error = new SummitError() { RejectCode = (SummitApiErrorCode)(-1), Message = "Device requested is not connected" } };
            }

            var response = new ImpedanceResponse
            {
                Name = request.Name,
                ImpedanceErrorCode = ImpedanceErrorCode.NoImpedanceError,
            };

            // First thing we should do is iterate though the pairs.
            List<List<Tuple<byte, byte>>> electrodeChunks = new List<List<Tuple<byte, byte>>>();
            List<Tuple<byte, byte>> currentChunk = new List<Tuple<byte, byte>>();

            foreach (var leadPair in request.LeadList)
            {
                if (currentChunk.Count() >= 16)
                {
                    electrodeChunks.Add(currentChunk);
                    currentChunk = new List<Tuple<byte, byte>>();
                }

                // We subtract one for the contacts because gRPC doesn't transmit 0s.
                byte electrode1 = (byte)(leadPair.Lead1 - 1);
                byte electrode2 = (byte)(leadPair.Lead2 - 1);

                if (electrode1 < 0 || electrode1 > 16 || electrode2 < 0 || electrode2 > 16)
                {
                    response.ImpedanceErrorCode = ImpedanceErrorCode.OutOfRangeLeadPairValues;
                    response.ErrorDescription = $"Electrode pair ({electrode1}, {electrode2}) out of bounds.";
                    return response;
                }

                currentChunk.Add(new Tuple<byte, byte>(electrode1, electrode2));
            }

            // We must ensure that sensing is off before initiating impedence tests.
            var status = device.Summit.WriteSensingState(SenseStates.None, 0);
            if (status.RejectCode != 0)
            {
                goto cleanup;
            }

            foreach (var chunk in electrodeChunks)
            {
                status = device.Summit.LeadIntegrityTest(chunk, out var integrityTestResult);

                if (status.RejectCode != 0)
                {
                    goto cleanup;
                }

                response.ImpedanceValues.AddRange(integrityTestResult.PairResults.Select(p => p.Impedance));
            }

            return response;

        cleanup:
            response.ImpedanceErrorCode = ImpedanceErrorCode.NullImpedance;
            response.Error = this.SummitToGrpcApiStatus(status);
            return response;
        }

        /// <summary>
        /// Enable Streams.
        /// </summary>
        /// <param name="request">Sense setting configuration and Name for device path.</param>
        /// <returns>SenseConfigurationResponse including error info.</returns>
        public StreamConfigureResponse EnableStream(StreamConfigureRequest request)
        {
            var (bridgeName, _) = URINameHelpers.ParseName(request.Name);
            var device = this.repository.GetConnectionByName(bridgeName);

            // Ensure the device is not null
            if (device == null)
            {
                return new StreamConfigureResponse() { Error = new SummitError() { RejectCode = (SummitApiErrorCode)(-1), Message = "Device requested is not connected" } };
            }

            bool timeDomainStream, fftStream, powerStream, accelerometryStream, detectorStream, adaptiveStateStream, loopRecordMarkerEchoStream, timeStream;

            if (request.Parameters != null)
            {
                var parameters = request.Parameters;

                timeDomainStream = parameters.EnableTimedomain;
                fftStream = parameters.EnableFft;
                powerStream = parameters.EnablePower;
                accelerometryStream = parameters.EnableAccelerometry;
                detectorStream = parameters.EnableDetector;
                adaptiveStateStream = parameters.EnableAdaptiveState;
                loopRecordMarkerEchoStream = parameters.EnableLoopRecordMarkerEcho;
                timeStream = parameters.EnableTime;
            }
            else
            {
                // The parameters could not be unpacked or wrong request parameter type: return failure.
                return new StreamConfigureResponse
                {
                    Name = request.Name,
                    StreamConfigureStatus = StreamConfigureStatus.Failure,
                    Error = new SummitError(),
                };
            }

            // Start Streaming
            var status = device.Summit.WriteSensingEnableStreams(timeDomainStream, fftStream, powerStream, detectorStream, adaptiveStateStream, accelerometryStream, timeStream, loopRecordMarkerEchoStream);
            if (status.RejectCode != 0)
            {
                return new StreamConfigureResponse
                {
                    Name = request.Name,
                    StreamConfigureStatus = StreamConfigureStatus.Failure,
                    Error = this.SummitToGrpcApiStatus(status),
                };
            }

            return new StreamConfigureResponse
            {
                Name = request.Name,
                StreamConfigureStatus = StreamConfigureStatus.Success,
                Error = this.SummitToGrpcApiStatus(status),
            };
        }

        /// <summary>
        /// Disable Streams.
        /// </summary>
        /// <param name="request">Sense setting configuration and Name for device path.</param>
        /// <returns>SenseConfigurationResponse including error info.</returns>
        public StreamConfigureResponse DisableStream(StreamConfigureRequest request)
        {
            var (bridgeName, _) = URINameHelpers.ParseName(request.Name);
            var device = this.repository.GetConnectionByName(bridgeName);

            // Ensure the device is not null
            if (device == null)
            {
                return new StreamConfigureResponse() { Error = new SummitError() { RejectCode = (SummitApiErrorCode)(-1), Message = "Device requested is not connected" } };
            }

            bool timeDomainStream, fftStream, powerStream, accelerometryStream, detectorStream, adaptiveStateStream, loopRecordMarkerEchoStream, timeStream;

            if (request.Parameters != null)
            {
                var parameters = request.Parameters;

                timeDomainStream = parameters.EnableTimedomain;
                fftStream = parameters.EnableFft;
                powerStream = parameters.EnablePower;
                accelerometryStream = parameters.EnableAccelerometry;
                detectorStream = parameters.EnableDetector;
                adaptiveStateStream = parameters.EnableAdaptiveState;
                loopRecordMarkerEchoStream = parameters.EnableLoopRecordMarkerEcho;
                timeStream = parameters.EnableTime;
            }
            else
            {
                // The parameters could not be unpacked or wrong request parameter type: return failure.
                return new StreamConfigureResponse
                {
                    Name = request.Name,
                    StreamConfigureStatus = StreamConfigureStatus.Failure,
                    Error = new SummitError(),
                };
            }

            // Start Streaming
            var status = device.Summit.WriteSensingDisableStreams(timeDomainStream, fftStream, powerStream, detectorStream, adaptiveStateStream, accelerometryStream, timeStream, loopRecordMarkerEchoStream);
            if (status.RejectCode != 0)
            {
                return new StreamConfigureResponse
                {
                    Name = request.Name,
                    StreamConfigureStatus = StreamConfigureStatus.Failure,
                    Error = this.SummitToGrpcApiStatus(status),
                };
            }

            return new StreamConfigureResponse
            {
                Name = request.Name,
                StreamConfigureStatus = StreamConfigureStatus.Success,
                Error = this.SummitToGrpcApiStatus(status),
            };
        }

        /// <summary>
        /// Configure beep behavior of bridge.
        /// </summary>
        /// <param name="request">Configure beep request.</param>
        /// <returns>Configure beep response.</returns>
        public ConfigureBeepResponse ConfigureBeep(ConfigureBeepRequest request)
        {
            var (bridgeName, _) = URINameHelpers.ParseName(request.Name);
            var device = this.repository.GetConnectionByName(bridgeName);

            // Ensure the device is not null
            if (device == null)
            {
                return new ConfigureBeepResponse() { Error = new SummitError() { RejectCode = (SummitApiErrorCode)(-1), Message = "Device requested is not connected" } };
            }

            CtmBeepEnables beepConfig = (CtmBeepEnables)request.BeepConfig;
            var status = device.Summit.WriteTelemetrySoundEnables(beepConfig);

            var response = new ConfigureBeepResponse
            {
                Name = request.Name,
                Error = this.SummitToGrpcApiStatus(status),
            };

            return response;
        }

        /// <summary>
        /// Generic method for querying bridges.
        /// </summary>
        /// <param name="request">The request query used to filter results.</param>
        /// <param name="deviceAddresses">The device addresses used to build the response.</param>
        /// <returns>A list of bridges.</returns>
        private List<OpenMind.Bridge> QueryBridges(QueryBridgesRequest request, IEnumerable<InstrumentInfo> deviceAddresses)
        {
            var response = new QueryBridgesResponse();
            var bridges = new List<OpenMind.Bridge>();

            foreach (var address in deviceAddresses)
            {
                var name = URINameHelpers.BuildNameFromSerialNumbers(address.SerialNumber);
                this.repository.CacheBridgeAddress(name, address);
                bridges.Add(new OpenMind.Bridge
                {
                    Name = name,
                });
            }

            if (request.Query != null)
            {
                return bridges.Where(bridge => bridge.Name.Contains(request.Query)).ToList();
            }
            else
            {
                return bridges;
            }
        }

        /// <summary>
        /// Translate Summit API status to gRPC.
        /// </summary>
        /// <param name="status">Summit API status.</param>
        /// <returns>gRPC API status.</returns>
        private SummitError SummitToGrpcApiStatus(APIReturnInfo status)
        {
            SummitErrorType theRejectCodeType = 0;
            switch (status.RejectCodeType.Name)
            {
                case "MasterRejectCode":
                    theRejectCodeType = SummitErrorType.MasterError;
                    break;
                case "APIRejectCodes":
                    theRejectCodeType = SummitErrorType.ApiError;
                    break;
                case "CommandResponseCodes":
                    theRejectCodeType = SummitErrorType.CommandError;
                    break;
                case "InstrumentReturnCode":
                    theRejectCodeType = SummitErrorType.InstrumentError;
                    break;
            }

            return new SummitError
            {
                ErrorType = theRejectCodeType,
                RejectCode = (SummitApiErrorCode)status.RejectCode,
                DeviceCommandCode = (SummitDeviceCommandCode)status.InsCommandCode,
                BridgeCommandCode = (SummitBridgeCommandCode)status.CtmCommandCode,
                Message = status.Descriptor,
                TxTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(status.TxTime, DateTimeKind.Utc)),
                RxTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(status.RxTime, DateTimeKind.Utc)),
                TransmitAttempts = status.TransmitAttempts,
                LinkStatus = Google.Protobuf.ByteString.CopyFrom(status.TheLinkStatus.GetBytes()),
            };
        }
    }
}

// <copyright file="DeviceManagerService.cs" company="OpenMind">
// Copyright (c) OpenMind. All rights reserved.
// </copyright>

namespace OpenMindServer.Services
{
    using System.Threading.Tasks;
    using Google.Protobuf.WellKnownTypes;
    using Grpc.Core;
    using OpenMind;

    /// <summary>
    /// The gRPC Device API.
    /// </summary>
    public class DeviceManagerService : OpenMind.DeviceManagerService.DeviceManagerServiceBase
    {
        /// <summary>
        /// The summitService used to translate calls between gRPC and Summit.
        /// </summary>
        private readonly SummitService summitService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceManagerService"/> class.
        /// </summary>
        /// <param name="summitService">gRPC to Summit translation service.</param>
        public DeviceManagerService(SummitService summitService)
        {
            this.summitService = summitService;
        }

        /// <summary>
        /// List devices endpoint.
        /// </summary>
        /// <param name="request">List devices request.</param>
        /// <param name="context">The call context.</param>
        /// <returns>List device response.</returns>
        public override Task<ListDeviceResponse> ListDevices(ListDeviceRequest request, ServerCallContext context)
        {
            return Task.FromResult(this.summitService.ListDevices(request));
        }

        /// <summary>
        /// Connect device endpoint.
        /// </summary>
        /// <param name="request">Connect device request.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Connect device response.</returns>
        public override Task<ConnectDeviceResponse> ConnectDevice(ConnectDeviceRequest request, ServerCallContext context)
        {
            return Task.FromResult(this.summitService.ConnectToDevice(request));
        }

        /// <summary>
        /// Disconnect device endpoint.
        /// </summary>
        /// <param name="request">Disconnect device request.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Nothing.</returns>
        public override Task<Empty> DisconnectDevice(DisconnectDeviceRequest request, ServerCallContext context)
        {
            return Task.FromResult(this.summitService.DisconnectFromDevice(request));
        }

        /// <summary>
        /// Time domain stream endpoint.
        /// </summary>
        /// <param name="request">Data stream enable request.</param>
        /// <param name="responseStream">The event stream.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Async task.</returns>
        public override Task TimeDomainStream(SetDataStreamEnable request, IServerStreamWriter<TimeDomainUpdate> responseStream, ServerCallContext context)
        {
            return this.summitService.CreateTimeDomainDataStream(request, responseStream);
        }

        /// <summary>
        /// Fourier transform stream endpoint.
        /// </summary>
        /// <param name="request">Data stream enable request.</param>
        /// <param name="responseStream">The event stream.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Async task.</returns>
        public override Task FourierTransformStream(SetDataStreamEnable request, IServerStreamWriter<FourierTransformUpdate> responseStream, ServerCallContext context)
        {
            return this.summitService.CreateFourierDataStream(request, responseStream);
        }

        /// <summary>
        /// Band power stream stream endpoint.
        /// </summary>
        /// <param name="request">Data stream enable request.</param>
        /// <param name="responseStream">The event stream.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Async task.</returns>
        public override Task BandPowerStream(SetDataStreamEnable request, IServerStreamWriter<BandPowerUpdate> responseStream, ServerCallContext context)
        {
            return this.summitService.CreateBandPowerDataStream(request, responseStream);
        }

        /// <summary>
        /// Adaptive stream stream endpoint.
        /// </summary>
        /// <param name="request">Data stream enable request.</param>
        /// <param name="responseStream">The event stream.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Async task.</returns>
        public override Task AdaptiveStream(SetDataStreamEnable request, IServerStreamWriter<AdaptiveUpdate> responseStream, ServerCallContext context)
        {
            return this.summitService.CreateAdaptiveDataStream(request, responseStream);
        }

        /// <summary>
        /// Intertial stream stream endpoint.
        /// </summary>
        /// <param name="request">Data stream enable request.</param>
        /// <param name="responseStream">The event stream.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Async task.</returns>
        public override Task InertialStream(SetDataStreamEnable request, IServerStreamWriter<InertialUpdate> responseStream, ServerCallContext context)
        {
            return this.summitService.CreateInertialDataStream(request, responseStream);
        }

        /// <summary>
        /// Loop record update stream stream endpoint.
        /// </summary>
        /// <param name="request">Data stream enable request.</param>
        /// <param name="responseStream">The event stream.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Async task.</returns>
        public override Task LoopRecordUpdateStream(SetDataStreamEnable request, IServerStreamWriter<LoopRecordUpdate> responseStream, ServerCallContext context)
        {
            return this.summitService.CreateLoopDataStream(request, responseStream);
        }

        /// <summary>
        /// Echo stream stream endpoint.
        /// </summary>
        /// <param name="request">Data stream enable request.</param>
        /// <param name="responseStream">The event stream.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Async task.</returns>
        public override Task EchoStream(SetDataStreamEnable request, IServerStreamWriter<EchoUpdate> responseStream, ServerCallContext context)
        {
            return this.summitService.CreateEchoDataStream(request, responseStream);
        }

        /// <summary>
        /// Device status endpoint.
        /// </summary>
        /// <param name="request">Device status request.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Device status response.</returns>
        public override Task<DeviceStatusResponse> DeviceStatus(DeviceStatusRequest request, ServerCallContext context)
        {
            return Task.FromResult(this.summitService.DeviceStatus(request));
        }

        /// <summary>
        /// Configure Sense endpoint.
        /// </summary>
        /// <param name="request">Sense configurations request.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Async task.</returns>
        public override Task<SenseConfigurationResponse> SenseConfiguration(SenseConfigurationRequest request, ServerCallContext context)
        {
            return Task.FromResult(this.summitService.ConfigureSense(request));
        }

        /// <summary>
        /// Get Impedance data.
        /// </summary>
        /// <param name="request">Get Integrity results.</param>
        /// <param name="context">The call context.</param>
        /// <returns>List device response.</returns>
        public override Task<ImpedanceResponse> LeadIntegrityTest(ImpedanceRequest request, ServerCallContext context)
        {
            return Task.FromResult(this.summitService.LeadIntegrityTest(request));
        }

        /// <summary>
        /// Enable Streams endpoint.
        /// </summary>
        /// <param name="request">Streams configurations request.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Async task.</returns>
        public override Task<StreamConfigureResponse> StreamEnable(StreamConfigureRequest request, ServerCallContext context)
        {
            return Task.FromResult(this.summitService.EnableStream(request));
        }

        /// <summary>
        /// Disable Streams endpoint.
        /// </summary>
        /// <param name="request">Streams configurations request.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Async task.</returns>
        public override Task<StreamConfigureResponse> StreamDisable(StreamConfigureRequest request, ServerCallContext context)
        {
            return Task.FromResult(this.summitService.DisableStream(request));
        }
    }
}

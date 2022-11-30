// <copyright file="SummitServiceInfo.cs" company="OpenMind">
// Copyright (c) OpenMind. All rights reserved.
// </copyright>

namespace OpenMindServer
{
    using System;
    using System.Collections.Concurrent;
    using Medtronic.NeuroStim.Olympus.DataTypes.Sensing;
    using Medtronic.NeuroStim.Olympus.DataTypes.Sensing.Packets;
    using Medtronic.SummitAPI.Classes;
    using Medtronic.SummitAPI.Events;
    using Medtronic.TelemetryM;
    using Medtronic.TelemetryM.CtmProtocol.Commands;
    using OpenMind;
    using OpenMindServer.Wrappers;

    /// <summary>
    /// A collection of extra data associated with the SumitSystem object.
    /// TODO: This could use a more informative name. It's not actually related to the SummitService class (which also needs a better name).
    /// </summary>
    public class SummitServiceInfo : IDisposable
    {
        /// <summary>
        /// The SummitManager associated with the SummitSystem.
        /// TODO: This should probably be a singleton.
        /// </summary>
        private readonly ISummitManager manager;

        /// <summary>
        /// Checks the CTM battery status for connection every n seconds
        /// TODO: Make this field configurable.
        /// </summary>
        private readonly int timeInSecondsToCheckCTMBatteryForConnection = 2;

        /// <summary>
        /// The URI formatted bridge name.
        /// </summary>
        private readonly string theBridgeName;

        /// <summary>
        /// The ConnectBridgeRequest that was used to create the bridge connection.
        /// We cache the request to make sure we can recreate the connections with
        /// the same parameters on disconnect.
        /// </summary>
        private readonly ConnectBridgeRequest summitConnectBridgeRequest;

        /// <summary>
        /// Counter to retry connection.
        /// </summary>
        private readonly int retryConnectionCounter;

        /// <summary>
        /// Whether the object has been disposed.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Whether the device name has been set.
        /// </summary>
        private bool deviceNameSet = false;

        /// <summary>
        /// The URI formatted device name.
        /// </summary>
        private string theDeviceSerial = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="SummitServiceInfo"/> class.
        /// </summary>
        /// <param name="bridgeName">The name of the bridg.</param>
        /// <param name="aSummitSystem">The SummitSystem cached connection.</param>
        /// <param name="aSummitManager">The SummitManager that created the cached SummitSystem, needed for disposal purposes.</param>
        /// <param name="connectBridgeRequest">The ConnectBridgeRequest used to create the initial bridge connection.</param>
        /// <param name="aAddress">The wireless address of the bridge.</param>
        public SummitServiceInfo(string bridgeName, ISummitSystem aSummitSystem, ISummitManager aSummitManager, ConnectBridgeRequest connectBridgeRequest, InstrumentInfo aAddress)
        {
            // Cache the SummitSystem objects provided to constructor
            this.theBridgeName = bridgeName;
            this.Summit = aSummitSystem;
            this.manager = aSummitManager;
            this.summitConnectBridgeRequest = connectBridgeRequest;
            this.Address = aAddress;
            this.retryConnectionCounter = connectBridgeRequest.Retries ?? 0;

            // Subscribe all event handlers
            this.Summit.UnexpectedCtmDisconnectHandler += this.TheSummitSystem_UnexpectedCtmDisconnectHandler;
            this.Summit.DataReceivedTDHandler += this.TheSummitSystem_DataReceivedTDHandler;
            this.Summit.DataReceivedFFTHandler += this.TheSummitSystem_DataReceivedFFTHandler;
            this.Summit.DataReceivedPowerHandler += this.TheSummitSystem_DataReceivedPowerHandler;
            this.Summit.DataReceivedAccelHandler += this.TheSummitSystem_DataReceivedAccelHandler;
            this.Summit.DataReceivedDetectorHandler += this.TheSummitSystem_DataReceivedDetectorHandler;
            this.Summit.DataReceivedLoopRecordUpdateHandler += this.TheSummitSystem_DataReceivedLoopRecordUpdateHandler;
            this.Summit.DataReceivedMarkerEchoHandler += this.TheSummitSystem_DataReceivedMarkerEchoHandler;
        }

        /// <summary>
        /// Event thrown upon unexpected disposal.
        /// </summary>
        public event EventHandler UnexpectedDisposalHandler;

        /// <summary>
        /// Gets or sets the SummitSystem implementation.
        /// </summary>
        public ISummitSystem Summit { get; set; }

        /// <summary>
        /// Gets or sets the wireless address of the bridge.
        /// </summary>
        public InstrumentInfo Address { get; set; }

        /// <summary>
        /// Gets or sets the queue containing the connection status events.
        /// </summary>
        public BlockingCollection<ConnectionUpdate> ConnectionStatusQueue { get; set; } = null;

        /// <summary>
        /// Gets or sets the queue containing the time domain event data.
        /// </summary>
        public BlockingCollection<TimeDomainUpdate> TimeDomainDataQueue { get; set; } = null;

        /// <summary>
        /// Gets or sets the queue containing the fourier transform event data.
        /// </summary>
        public BlockingCollection<FourierTransformUpdate> FourierDataQueue { get; set; } = null;

        /// <summary>
        /// Gets or sets the queue containing the band power event data.
        /// </summary>
        public BlockingCollection<BandPowerUpdate> BandPowerDataQueue { get; set; } = null;

        /// <summary>
        /// Gets or sets the queue containing the adaptive control event data.
        /// </summary>
        public BlockingCollection<AdaptiveUpdate> AdaptiveDataQueue { get; set; } = null;

        /// <summary>
        /// Gets or sets the queue containing the accelerometer event data.
        /// </summary>
        public BlockingCollection<InertialUpdate> InertialDataQueue { get; set; } = null;

        /// <summary>
        /// Gets or sets the queue containing the loop record event data.
        /// </summary>
        public BlockingCollection<LoopRecordUpdate> LoopRecordQueue { get; set; } = null;

        /// <summary>
        /// Gets or sets the queue containing the echo event data.
        /// </summary>
        public BlockingCollection<EchoUpdate> EchoDataQueue { get; set; } = null;

        /// <summary>
        /// Gets a value indicating whether the SummitServiceInfo is disposed.
        /// </summary>
        public bool IsDisposed { get => this.disposed; }

        /// <summary>
        /// Gets the URI formatted name of the associated SummitSystem.
        /// </summary>
        public string Name { get => URINameHelpers.BuildNameFromBridgeNameDeviceSerial(this.theBridgeName, this.theDeviceSerial); }

        /// <summary>
        /// TODO: What is this used for? Should we remove? Is not used anywhere except in tests.
        /// Sets the device name.
        /// </summary>
        /// <param name="deviceSerial">The URI formatted device name.</param>
        public void SetDeviceSerial(string deviceSerial)
        {
            if (!this.deviceNameSet)
            {
                this.deviceNameSet = true;
                this.theDeviceSerial = deviceSerial;
            }
        }

        /// <summary>
        /// Gets the device name.
        /// </summary>
        /// <returns>The URI formatted device name.</returns>
        public string GetDeviceSerial()
        {
            return this.theDeviceSerial;
        }

        /// <summary>
        /// Disposes the class resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Virtual IDisposable Call Override with Summit Disposal functionality.
        /// </summary>
        /// <param name="disposing">indicate if disposal should occur or not, part of IDisposable Dispose definition.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Check if this instance has already been disposed
            if (!this.disposed)
            {
                // Set Repo Object to disposed
                this.disposed = true;

                // Turn off streaming layers
                if (this.ConnectionStatusQueue != null)
                {
                    this.ConnectionStatusQueue.CompleteAdding();
                }

                if (this.TimeDomainDataQueue != null)
                {
                    this.TimeDomainDataQueue.CompleteAdding();
                }

                if (this.FourierDataQueue != null)
                {
                    this.FourierDataQueue.CompleteAdding();
                }

                if (this.BandPowerDataQueue != null)
                {
                    this.BandPowerDataQueue.CompleteAdding();
                }

                if (this.AdaptiveDataQueue != null)
                {
                    this.AdaptiveDataQueue.CompleteAdding();
                }

                if (this.InertialDataQueue != null)
                {
                    this.InertialDataQueue.CompleteAdding();
                }

                if (this.LoopRecordQueue != null)
                {
                    this.LoopRecordQueue.CompleteAdding();
                }

                if (this.EchoDataQueue != null)
                {
                    this.EchoDataQueue.CompleteAdding();
                }

                // Check if the SummitSystem is already disposed
                if (!this.Summit.IsDisposed)
                {
                    this.manager.DisposeSummit(this.Summit);
                }
            }
        }

        /// <summary>
        /// Logic for disconnect handling.
        /// </summary>
        /// <param name="sender">The SummitSystem emitting the connection events.</param>
        /// <param name="e">It doesn't seem that the Summit API sends any useful information in the event arg.</param>
        private void TheSummitSystem_UnexpectedCtmDisconnectHandler(object sender, EventArgs e)
        {
            ISummitSystem localSummit = this.Summit;
            APIReturnInfo returnInfo = localSummit.ReadBatteryLevel(out var batteryStatusResult);
            bool flagToAddToStatusQueueOnlyOnce = true;
            while (returnInfo.RejectCode != 0)
            {
                // Indicate global enum that we are in danger zone
                // Add to connection stream that we are in danger zone
                // *** Example of adding a message to the connection stream
                if (this.ConnectionStatusQueue != null && flagToAddToStatusQueueOnlyOnce)
                {
                    this.ConnectionStatusQueue.Add(new ConnectionUpdate() { ConnectionStatus = "CTM Disconnected!", Name = this.Name });
                    flagToAddToStatusQueueOnlyOnce = false;
                }

                if (localSummit.IsDisposed)
                {
                    // Indicate global enum that we are disposed
                    // Add to connection stream output “Exit Danger Zone: Failure” status
                    if (this.ConnectionStatusQueue != null)
                    {
                        this.ConnectionStatusQueue.Add(new ConnectionUpdate() { ConnectionStatus = "CTM Disposed!", Name = this.Name });
                    }

                    // Attempt to reconnect
                    if (this.ReconnectionOnUnexpectedDisposed(this.retryConnectionCounter))
                    {
                        break;
                    }
                    else
                    {
                        // Failed to reconnect in specified number of retries, prepare for disposing.
                        if (this.ConnectionStatusQueue != null)
                        {
                            this.ConnectionStatusQueue.Add(new ConnectionUpdate() { ConnectionStatus = "SummitSystem Reconnection Failed!", Name = this.Name });
                            flagToAddToStatusQueueOnlyOnce = false;
                        }

                        // Dispose of this info object, inform repository to remove the connection.
                        this.Dispose();
                        this?.UnexpectedDisposalHandler.Invoke(this, new EventArgs());
                        return;
                    }
                }
                else
                {
                    // Maybe put in try/catch blocks to avoid the moment after checking if(isDisposed) it actually disposes and errors on this call
                    returnInfo = localSummit.ReadBatteryLevel(out batteryStatusResult);
                    System.Threading.Thread.Sleep(this.timeInSecondsToCheckCTMBatteryForConnection);
                }
            }

            // Add to connection stream output “Exit Danger Zone: Success” status
            if (this.ConnectionStatusQueue != null)
            {
                this.ConnectionStatusQueue.Add(new ConnectionUpdate() { ConnectionStatus = "SummitSystem Reconnected!", Name = this.Name });
                flagToAddToStatusQueueOnlyOnce = false;
            }
        }

        /// <summary>
        /// Event handler for Summit Time Domain, pushes data into queue for gRPC endpoint streaming.
        /// </summary>
        /// <param name="sender">Generic Event Marker Sender.</param>
        /// <param name="receivedData">Received data from the Summit Device.</param>
        private void TheSummitSystem_DataReceivedTDHandler(object sender, SensingEventTD receivedData)
        {
            if (this.TimeDomainDataQueue != null)
            {
                // Make sure PacketGenTime is valid
                long systemEstimateTime = 0;
                if (receivedData.GenerationTimeEstimate.Ticks != 0)
                {
                    systemEstimateTime = receivedData.GenerationTimeEstimate.ToFileTimeUtc();
                }

                // Create a buffer to map time domain data into gRPC
                TimeDomainUpdate theNewData = new TimeDomainUpdate()
                {
                    Name = this.Name,
                    Data = { },
                    Header = new DataPacketHeader()
                    {
                        DataTypeSequenceNumber = receivedData.Header.DataTypeSequence,
                        GlobalSequenceNumber = receivedData.Header.GlobalSequence,
                        SystemEstDeviceTxTime = systemEstimateTime,
                        SystemRxTime = receivedData.PacketRxTime.ToFileTimeUtc(),
                        InsTick = receivedData.Header.SystemTick,
                        InsTimestamp = receivedData.Header.Timestamp.Seconds,
                    },
                };

                // Map sample rate from device provided enum to a double value
                double sampleRateHz = -1;
                switch (receivedData.SampleRate)
                {
                    case TdSampleRates.Sample0250Hz:
                        sampleRateHz = 250.0;
                        break;
                    case TdSampleRates.Sample0500Hz:
                        sampleRateHz = 500.0;
                        break;
                    case TdSampleRates.Sample1000Hz:
                        sampleRateHz = 1000.0;
                        break;
                    default:
                        break;
                }

                // Now assemble the Data object
                foreach (SenseTimeDomainChannel channelName in (SenseTimeDomainChannel[])Enum.GetValues(typeof(SenseTimeDomainChannel)))
                {
                    if (receivedData.ChannelSamples.TryGetValue(channelName, out var newDataBuffer))
                    {
                        TimeDomainChannelData aTimeDomainChannelData = new TimeDomainChannelData()
                        {
                            ChannelData = { },
                            ChannelId = channelName.ToString("g"),
                            SamplingRate = sampleRateHz,
                            Units = receivedData.Units,
                            StimulationPulseIndeces = { },
                        };

                        aTimeDomainChannelData.ChannelData.AddRange(newDataBuffer);

                        // add in the evoked markers
                        if (receivedData.EvokedMarker.TryGetValue(channelName, out var evokedMarkerList))
                        {
                            foreach (EvokedResponseMarker aMarker in evokedMarkerList)
                            {
                                aTimeDomainChannelData.StimulationPulseIndeces.Add(new TimeDomainChannelStimTiming() { DataIndex = aMarker.IndexOfDataPointFollowing, TimeBeforeInMicroseconds = aMarker.TimeBeforeDataPointInMicroSeconds });
                            }
                        }

                        theNewData.Data.Add(aTimeDomainChannelData);
                    }
                }

                // Add the created buffer object to the queue
                this.TimeDomainDataQueue.Add(theNewData);
            }
        }

        /// <summary>
        /// Event handler for Summit FFT Updates, pushes data into queue for gRPC endpoint streaming.
        /// </summary>
        /// <param name="sender">Generic Event Marker Sender.</param>
        /// <param name="receivedData">Received data from the Summit Device.</param>
        private void TheSummitSystem_DataReceivedFFTHandler(object sender, SensingEventFFT receivedData)
        {
            if (this.FourierDataQueue != null)
            {
                // Make sure PacketGenTime is valid
                long systemEstimateTime = 0;
                if (receivedData.GenerationTimeEstimate.Ticks != 0)
                {
                    systemEstimateTime = receivedData.GenerationTimeEstimate.ToFileTimeUtc();
                }

                // Create a buffer to map fft data into gRPC
                FourierTransformUpdate theNewData = new FourierTransformUpdate()
                {
                    Name = this.Name,
                    Data = { },
                    Header = new DataPacketHeader()
                    {
                        DataTypeSequenceNumber = receivedData.Header.DataTypeSequence,
                        GlobalSequenceNumber = receivedData.Header.GlobalSequence,
                        SystemEstDeviceTxTime = systemEstimateTime,
                        SystemRxTime = receivedData.PacketRxTime.ToFileTimeUtc(),
                        InsTick = receivedData.Header.SystemTick,
                        InsTimestamp = receivedData.Header.Timestamp.Seconds,
                    },
                };

                // Map sample rate from device provided enum to a double value
                double sampleRateHz = -1;
                switch (receivedData.SampleRate)
                {
                    case TdSampleRates.Sample0250Hz:
                        sampleRateHz = 250.0;
                        break;
                    case TdSampleRates.Sample0500Hz:
                        sampleRateHz = 500.0;
                        break;
                    case TdSampleRates.Sample1000Hz:
                        sampleRateHz = 1000.0;
                        break;
                    default:
                        break;
                }

                // Map FFT length from device provided enum to a uint value
                uint fftLength = 0;
                switch (receivedData.FftSize)
                {
                    case FftSizes.Size0064:
                        fftLength = 64;
                        break;
                    case FftSizes.Size0256:
                        fftLength = 256;
                        break;
                    case FftSizes.Size1024:
                        fftLength = 1024;
                        break;
                    default:
                        break;
                }

                // Now assemble the Data object
                FourierTransformChannelData theNewFFTData = new FourierTransformChannelData()
                {
                    ChannelId = receivedData.Channel.ToString("g"),
                    FourierLength = fftLength,
                    SamplingRate = sampleRateHz,
                    Units = string.Empty,
                    ChannelData = { },
                };
                theNewFFTData.ChannelData.AddRange(receivedData.FftOutput);
                theNewData.Data.Add(theNewFFTData);

                // Add the created buffer object to the queue
                this.FourierDataQueue.Add(theNewData);
            }
        }

        /// <summary>
        /// Event handler for Summit Band Power Updates, pushes data into queue for gRPC endpoint streaming.
        /// </summary>
        /// <param name="sender">Generic Event Marker Sender.</param>
        /// <param name="receivedData">Received data from the Summit Device.</param>
        private void TheSummitSystem_DataReceivedPowerHandler(object sender, SensingEventPower receivedData)
        {
            if (this.BandPowerDataQueue != null)
            {
                // Make sure PacketGenTime is valid
                long systemEstimateTime = 0;
                if (receivedData.GenerationTimeEstimate.Ticks != 0)
                {
                    systemEstimateTime = receivedData.GenerationTimeEstimate.ToFileTimeUtc();
                }

                // Create a buffer to map time domain data into gRPC
                BandPowerUpdate theNewData = new BandPowerUpdate()
                {
                    Name = this.Name,
                    Data = { },
                    Header = new DataPacketHeader()
                    {
                        DataTypeSequenceNumber = receivedData.Header.DataTypeSequence,
                        GlobalSequenceNumber = receivedData.Header.GlobalSequence,
                        SystemEstDeviceTxTime = systemEstimateTime,
                        SystemRxTime = receivedData.PacketRxTime.ToFileTimeUtc(),
                        InsTick = receivedData.Header.SystemTick,
                        InsTimestamp = receivedData.Header.Timestamp.Seconds,
                    },
                };

                // Map sample rate from device provided enum to a double value
                double sampleRateHz = -1;
                switch (receivedData.SampleRate)
                {
                    case TdSampleRates.Sample0250Hz:
                        sampleRateHz = 250.0;
                        break;
                    case TdSampleRates.Sample0500Hz:
                        sampleRateHz = 500.0;
                        break;
                    case TdSampleRates.Sample1000Hz:
                        sampleRateHz = 1000.0;
                        break;
                    default:
                        break;
                }

                // Map FFT length from device provided enum to a uint value
                uint fftLength = 0;
                switch (receivedData.FftSize)
                {
                    case FftSizes.Size0064:
                        fftLength = 64;
                        break;
                    case FftSizes.Size0256:
                        fftLength = 256;
                        break;
                    case FftSizes.Size1024:
                        fftLength = 1024;
                        break;
                    default:
                        break;
                }

                // Now assemble the Data object
                for (int i = 0; i < receivedData.Bands.Count; i++)
                {
                        BandPowerChannelData aBandPowerChannelData = new BandPowerChannelData()
                        {
                            ChannelData = receivedData.Bands[i],
                            ChannelId = i.ToString("g"),
                            SamplingRate = sampleRateHz,
                            Units = string.Empty,
                            FourierLength = fftLength,
                        };

                        theNewData.Data.Add(aBandPowerChannelData);
                }

                // Add the created buffer object to the queue
                this.BandPowerDataQueue.Add(theNewData);
            }
        }

        /// <summary>
        /// Event handler for Summit Detector Updates, pushes data into queue for gRPC endpoint streaming.
        /// </summary>
        /// <param name="sender">Generic Event Marker Sender.</param>
        /// <param name="receivedData">Received data from the Summit Device.</param>
        private void TheSummitSystem_DataReceivedDetectorHandler(object sender, AdaptiveDetectEvent receivedData)
        {
            if (this.AdaptiveDataQueue != null)
            {
                // Make sure PacketGenTime is valid
                long systemEstimateTime = 0;
                if (receivedData.GenerationTimeEstimate.Ticks != 0)
                {
                    systemEstimateTime = receivedData.GenerationTimeEstimate.ToFileTimeUtc();
                }

                // Create a buffer to map adaptive data into gRPC
                AdaptiveUpdate theNewData = new AdaptiveUpdate()
                {
                    Name = this.Name,
                    Header = new DataPacketHeader()
                    {
                        DataTypeSequenceNumber = receivedData.Header.DataTypeSequence,
                        GlobalSequenceNumber = receivedData.Header.GlobalSequence,
                        SystemEstDeviceTxTime = systemEstimateTime,
                        SystemRxTime = receivedData.PacketRxTime.ToFileTimeUtc(),
                        InsTick = receivedData.Header.SystemTick,
                        InsTimestamp = receivedData.Header.Timestamp.Seconds,
                    },
                    CurrentState = receivedData.CurrentAdaptiveState,
                    CurrentStateCount = receivedData.StateEntryCount,
                    CurrentStateTotalTime = receivedData.StateTime,
                    PreviousState = receivedData.PreviousAdaptiveState,
                    CurrentFrequency = receivedData.StimRateInHz,
                    InHoldoff = receivedData.IsInHoldOffOnStartup,
                    CurrentAmplitudes = { },
                    DetectorState = { (DetectorState)receivedData.Ld0DetectionStatus, (DetectorState)receivedData.Ld1DetectionStatus },
                    DetectorStatus =
                    {
                        new DetectorStatus()
                        {
                            FeatureInputs = { },
                            Output = receivedData.Ld0Status.Output,
                            LowThreshold = receivedData.Ld0Status.LowThreshold,
                            HighThreshold = receivedData.Ld0Status.HighThreshold,
                            FixedDecimalSetpoint = receivedData.Ld0Status.FixedDecimalPoint,
                        },
                        new DetectorStatus()
                        {
                            FeatureInputs = { },
                            Output = receivedData.Ld1Status.Output,
                            LowThreshold = receivedData.Ld1Status.LowThreshold,
                            HighThreshold = receivedData.Ld1Status.HighThreshold,
                            FixedDecimalSetpoint = receivedData.Ld1Status.FixedDecimalPoint,
                        },
                    },
                    SensorsEnabled = { },
                    AdaptiveStimRamping = { },
                };

                theNewData.CurrentAmplitudes.AddRange(receivedData.CurrentProgramAmplitudesInMilliamps);
                theNewData.DetectorStatus[0].FeatureInputs.AddRange(receivedData.Ld0Status.FeatureInputs);
                theNewData.DetectorStatus[1].FeatureInputs.AddRange(receivedData.Ld1Status.FeatureInputs);

                // roll through the flag-enums in C# and created repeated fields since protobuf doesn't support flag enums.
                foreach (SenseStates sensor in (SenseStates[])Enum.GetValues(typeof(SenseStates)))
                {
                    if (receivedData.SensingStatus.HasFlag(sensor))
                    {
                        theNewData.SensorsEnabled.Add((SensingEnables)sensor);
                    }
                }

                // If more than one enum flag was added, remove the default 'zero'
                if (theNewData.SensorsEnabled.Count > 1)
                {
                    theNewData.SensorsEnabled.Remove(SensingEnables.SensingNone);
                }

                foreach (AdaptiveTherapyStatusFlags adaptiveFlags in (AdaptiveTherapyStatusFlags[])Enum.GetValues(typeof(AdaptiveTherapyStatusFlags)))
                {
                    if (receivedData.StimFlags.HasFlag(adaptiveFlags))
                    {
                        theNewData.AdaptiveStimRamping.Add((AdaptiveRampingFlags)adaptiveFlags);
                    }
                }

                // If more than one enum flag was added, remove the default 'zero'
                if (theNewData.AdaptiveStimRamping.Count > 1)
                {
                    theNewData.AdaptiveStimRamping.Remove(AdaptiveRampingFlags.RampingNone);
                }

                // Add the created buffer object to the queue
                this.AdaptiveDataQueue.Add(theNewData);
            }
        }

        /// <summary>
        /// Event handler for Summit Accelerometer Updates, pushes data into queue for gRPC endpoint streaming.
        /// </summary>
        /// <param name="sender">Generic Event Marker Sender.</param>
        /// <param name="receivedData">Received data from the Summit Device.</param>
        private void TheSummitSystem_DataReceivedAccelHandler(object sender, SensingEventAccel receivedData)
        {
            if (this.InertialDataQueue != null)
            {
                // Make sure PacketGenTime is valid
                long systemEstimateTime = 0;
                if (receivedData.GenerationTimeEstimate.Ticks != 0)
                {
                    systemEstimateTime = receivedData.GenerationTimeEstimate.ToFileTimeUtc();
                }

                // Create a buffer to map inertial data into gRPC
                InertialUpdate theNewData = new InertialUpdate()
                {
                    Name = this.Name,
                    Data =
                    {
                        new InertialChannelData()
                        {
                            ChannelId = "Accelerometer",
                            Units = receivedData.Units,
                            ChannelAxis = imu_axis.X,
                            ChannelData = { },
                        },
                        new InertialChannelData()
                        {
                            ChannelId = "Accelerometer",
                            Units = receivedData.Units,
                            ChannelAxis = imu_axis.Y,
                            ChannelData = { },
                        },
                        new InertialChannelData()
                        {
                            ChannelId = "Accelerometer",
                            Units = receivedData.Units,
                            ChannelAxis = imu_axis.Z,
                            ChannelData = { },
                        },
                    },
                    Header = new DataPacketHeader()
                    {
                        DataTypeSequenceNumber = receivedData.Header.DataTypeSequence,
                        GlobalSequenceNumber = receivedData.Header.GlobalSequence,
                        SystemEstDeviceTxTime = systemEstimateTime,
                        SystemRxTime = receivedData.PacketRxTime.ToFileTimeUtc(),
                        InsTick = receivedData.Header.SystemTick,
                        InsTimestamp = receivedData.Header.Timestamp.Seconds,
                    },
                };

                // Map sample rate from device provided enum to a double value
                switch (receivedData.SampleRate)
                {
                    case AccelSampleRate.Sample04:
                        theNewData.Data[0].SamplingRate = 4.0;
                        theNewData.Data[1].SamplingRate = 4.0;
                        theNewData.Data[2].SamplingRate = 4.0;
                        break;
                    case AccelSampleRate.Sample08:
                        theNewData.Data[0].SamplingRate = 8.0;
                        theNewData.Data[1].SamplingRate = 8.0;
                        theNewData.Data[2].SamplingRate = 8.0;
                        break;
                    case AccelSampleRate.Sample16:
                        theNewData.Data[0].SamplingRate = 16.0;
                        theNewData.Data[1].SamplingRate = 16.0;
                        theNewData.Data[2].SamplingRate = 16.0;
                        break;
                    case AccelSampleRate.Sample32:
                        theNewData.Data[0].SamplingRate = 32.0;
                        theNewData.Data[1].SamplingRate = 32.0;
                        theNewData.Data[2].SamplingRate = 32.0;
                        break;
                    case AccelSampleRate.Sample64:
                        theNewData.Data[0].SamplingRate = 64.0;
                        theNewData.Data[1].SamplingRate = 64.0;
                        theNewData.Data[2].SamplingRate = 64.0;
                        break;
                    default:
                        theNewData.Data[0].SamplingRate = -1;
                        theNewData.Data[1].SamplingRate = -1;
                        theNewData.Data[2].SamplingRate = -1;
                        break;
                }

                // Load in the data
                theNewData.Data[0].ChannelData.AddRange(receivedData.XSamples);
                theNewData.Data[1].ChannelData.AddRange(receivedData.YSamples);
                theNewData.Data[2].ChannelData.AddRange(receivedData.ZSamples);

                // Add the created buffer object to the queue
                this.InertialDataQueue.Add(theNewData);
            }
        }

        /// <summary>
        /// Event handler for Summit Loop Record Updates, pushes data into queue for gRPC endpoint streaming.
        /// </summary>
        /// <param name="sender">Generic Event Marker Sender.</param>
        /// <param name="receivedData">Received data from the Summit Device.</param>
        private void TheSummitSystem_DataReceivedLoopRecordUpdateHandler(object sender, LoopRecordUpdateEvent receivedData)
        {
            if (this.LoopRecordQueue != null)
            {
                // Make sure PacketGenTime is valid
                long systemEstimateTime = 0;
                if (receivedData.GenerationTimeEstimate.Ticks != 0)
                {
                    systemEstimateTime = receivedData.GenerationTimeEstimate.ToFileTimeUtc();
                }

                // Create a buffer to map loop record data into gRPC
                DataPacketHeader headerBuffer;
                if (receivedData.Header != null)
                {
                    headerBuffer = new DataPacketHeader()
                    {
                        DataTypeSequenceNumber = receivedData.Header.DataTypeSequence,
                        GlobalSequenceNumber = receivedData.Header.GlobalSequence,
                        SystemEstDeviceTxTime = systemEstimateTime,
                        InsTick = receivedData.Header.SystemTick,
                        SystemRxTime = receivedData.PacketRxTime.ToFileTimeUtc(),
                        InsTimestamp = receivedData.Header.Timestamp.Seconds,
                    };
                }
                else
                {
                    headerBuffer = new DataPacketHeader()
                    {
                        DataTypeSequenceNumber = 0,
                        GlobalSequenceNumber = 0,
                        SystemEstDeviceTxTime = systemEstimateTime,
                        InsTick = 0,
                        SystemRxTime = receivedData.PacketRxTime.ToFileTimeUtc(),
                        InsTimestamp = 0,
                    };
                }

                // Create the loop
                LoopRecordUpdate theNewData = new LoopRecordUpdate()
                {
                    Name = this.Name,
                    Triggers = { },
                    Header = headerBuffer,
                    Flags = { },
                };

                // roll through the flag-enums in C# and created repeated fields since protobuf doesn't support flag enums.
                foreach (LoopRecordingFlags aFlag in (LoopRecordingFlags[])Enum.GetValues(typeof(LoopRecordingFlags)))
                {
                    if (receivedData.LoopRecordStatusFlags.HasFlag(aFlag))
                    {
                        theNewData.Flags.Add((OpenMind.LoopRecordFlags)aFlag);
                    }
                }

                // If more than one enum flag was added, remove the default 'zero'
                if (theNewData.Flags.Count > 1)
                {
                    theNewData.Flags.Remove(LoopRecordFlags.NoFlags);
                }

                foreach (var aTrigger in (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.LoopRecordingTriggers[])Enum.GetValues(typeof(Medtronic.NeuroStim.Olympus.DataTypes.Sensing.LoopRecordingTriggers)))
                {
                    if (receivedData.LoopRecordTriggers.HasFlag(aTrigger))
                    {
                        theNewData.Triggers.Add((OpenMind.LoopRecordTriggers)aTrigger);
                    }
                }

                // If more than one enum flag was added, remove the default 'zero'
                if (theNewData.Triggers.Count > 1)
                {
                    theNewData.Triggers.Remove(LoopRecordTriggers.NoTrigger);
                }

                // Add the created buffer object to the queue
                this.LoopRecordQueue.Add(theNewData);
            }
        }

        /// <summary>
        /// Event handler for Summit Echo Markers, pushes data into queue for gRPC endpoint streaming.
        /// </summary>
        /// <param name="sender">Generic Event Marker Sender.</param>
        /// <param name="receivedData">Received data from the Summit Device.</param>
        private void TheSummitSystem_DataReceivedMarkerEchoHandler(object sender, ExternalMarkerEchoEvent receivedData)
        {
            if (this.EchoDataQueue != null)
            {
                // Make sure PacketGenTime is valid
                long systemEstimateTime = 0;
                if (receivedData.GenerationTimeEstimate.Ticks != 0)
                {
                    systemEstimateTime = receivedData.GenerationTimeEstimate.ToFileTimeUtc();
                }

                // Create a buffer to map loop record data into gRPC
                DataPacketHeader headerBuffer;
                if (receivedData.Header != null)
                {
                    headerBuffer = new DataPacketHeader()
                    {
                        DataTypeSequenceNumber = receivedData.Header.DataTypeSequence,
                        GlobalSequenceNumber = receivedData.Header.GlobalSequence,
                        SystemEstDeviceTxTime = systemEstimateTime,
                        InsTick = receivedData.Header.SystemTick,
                        SystemRxTime = receivedData.PacketRxTime.ToFileTimeUtc(),
                        InsTimestamp = receivedData.Header.Timestamp.Seconds,
                    };
                }
                else
                {
                    headerBuffer = new DataPacketHeader()
                    {
                        DataTypeSequenceNumber = 0,
                        GlobalSequenceNumber = 0,
                        SystemEstDeviceTxTime = systemEstimateTime,
                        InsTick = 0,
                        SystemRxTime = receivedData.PacketRxTime.ToFileTimeUtc(),
                        InsTimestamp = 0,
                    };
                }

                // Create a buffer to map loop record data into gRPC
                EchoUpdate theNewData = new EchoUpdate()
                {
                    Name = this.Name,
                    EchoByte = receivedData.TheMarkerByte,
                    Header = headerBuffer,
                };

                // Add the created buffer object to the queue
                this.EchoDataQueue.Add(theNewData);
            }
        }

        /// <summary>
        /// Reconnect when summit is disposed.
        /// </summary>
        /// <returns>True if reconnect was successful, false otherwise.</returns>
        private bool ReconnectionOnUnexpectedDisposed(int numberOfRetries)
        {
            // Reconnect based on number of times requested by user as part of instance bring-up.
            string reconnectErrorSource;
            while (numberOfRetries-- > 0)
            {
                // Attempt to connect
                if (this.ReconnectionOnUnexpectedDisposedHelper(out reconnectErrorSource))
                {
                    return true;
                }

                // Add to connection stream that we failed a connection attempt
                if (this.ConnectionStatusQueue != null)
                {
                    this.ConnectionStatusQueue.Add(new ConnectionUpdate()
                    {
                        ConnectionStatus = reconnectErrorSource + " Retry Failed! " + numberOfRetries + " tries remaining.",
                        Name = this.Name,
                    });
                }

                // Pause before re-attempting.
                System.Threading.Thread.Sleep(5000);
            }

            // Failure to connect in specified number of retries. Return False.
            return false;
        }

        /// <summary>
        /// Reconnect logic after unexpected disconnect.
        /// </summary>
        /// <returns>True if reconnect was successful, false otherwise.</returns>
        private bool ReconnectionOnUnexpectedDisposedHelper(out string errorSource)
        {
            var summitConnectBridgeParameters = this.summitConnectBridgeRequest;
            ISummitSystem bufferSummit;
            ushort telemetryMode = 3;
            if (summitConnectBridgeParameters.TelemetryMode == "4")
            {
                telemetryMode = 4;
            }

            var summitConnectionStatus = this.manager.CreateSummit(
                out bufferSummit,
                this.Address,
                (InstrumentPhysicalLayers)summitConnectBridgeParameters.PhysicalLayer,
                telemetryMode,
                (byte)summitConnectBridgeParameters.TelemetryRatio,
                (CtmBeepEnables)summitConnectBridgeParameters.BeepEnables);

            if (summitConnectionStatus != ManagerConnectStatus.Success)
            {
                errorSource = "CTM";
                return false;
            }

            APIReturnInfo connectReturn = bufferSummit.StartInsSession(null, out ConnectReturn theWarnings, true);

            if (connectReturn.RejectCode == 0)
            {
                this.Summit = bufferSummit;
                this.Summit.UnexpectedCtmDisconnectHandler += this.TheSummitSystem_UnexpectedCtmDisconnectHandler;
                this.Summit.DataReceivedTDHandler += this.TheSummitSystem_DataReceivedTDHandler;
                this.Summit.DataReceivedFFTHandler += this.TheSummitSystem_DataReceivedFFTHandler;
                this.Summit.DataReceivedPowerHandler += this.TheSummitSystem_DataReceivedPowerHandler;
                this.Summit.DataReceivedAccelHandler += this.TheSummitSystem_DataReceivedAccelHandler;
                this.Summit.DataReceivedDetectorHandler += this.TheSummitSystem_DataReceivedDetectorHandler;
                this.Summit.DataReceivedLoopRecordUpdateHandler += this.TheSummitSystem_DataReceivedLoopRecordUpdateHandler;
                this.Summit.DataReceivedMarkerEchoHandler += this.TheSummitSystem_DataReceivedMarkerEchoHandler;
                errorSource = string.Empty;
                return true;
            }
            else
            {
                this.manager.DisposeSummit(bufferSummit);
                errorSource = "INS";
                return false;
            }
        }
    }
}

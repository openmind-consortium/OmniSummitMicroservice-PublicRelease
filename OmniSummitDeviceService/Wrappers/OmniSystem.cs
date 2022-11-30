// <copyright file="OmniSystem.cs" company="OpenMind">
// Copyright (c) OpenMind. All rights reserved.
// </copyright>

namespace OpenMindServer.Wrappers
{
    using System;
    using System.Collections.Generic;
    using Medtronic.NeuroStim.Olympus.DataTypes.DeviceManagement;
    using Medtronic.NeuroStim.Olympus.DataTypes.Measurement;
    using Medtronic.NeuroStim.Olympus.DataTypes.PowerManagement;
    using Medtronic.NeuroStim.Olympus.DataTypes.Sensing;
    using Medtronic.SummitAPI.Classes;
    using Medtronic.SummitAPI.Events;
    using Medtronic.TelemetryM;
    using Medtronic.TelemetryM.CtmProtocol.Commands;

    /// <summary>
    /// Proxy for SummitSystem. Implements the ISummitSystem interface which helps with testing.
    /// </summary>
    public class OmniSystem : ISummitSystem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OmniSystem"/> class.
        /// </summary>
        /// <param name="summitSystem">The SummitSystem to proxy calls to.</param>
        public OmniSystem(SummitSystem summitSystem)
        {
            this.SummitSystem = summitSystem;
        }

        // NOTE (BNR): We don't document this stuff because it's just wrapping the SummitSystem
        //             which already has ample documentation. Perhaps there's a way to link
        //             documentation between this class and SummitSystem?
#pragma warning disable SA1600 // Elements should be documented
        public event EventHandler UnexpectedCtmDisconnectHandler
        {
            add
            {
                this.SummitSystem.UnexpectedCtmDisconnectHandler += value;
            }

            remove
            {
                this.SummitSystem.UnexpectedCtmDisconnectHandler -= value;
            }
        }

        public event EventHandler<SensingEventTD> DataReceivedTDHandler
        {
            add
            {
                this.SummitSystem.DataReceivedTDHandler += value;
            }

            remove
            {
                this.SummitSystem.DataReceivedTDHandler -= value;
            }
        }

        public event EventHandler<SensingEventFFT> DataReceivedFFTHandler
        {
            add
            {
                this.SummitSystem.DataReceivedFFTHandler += value;
            }

            remove
            {
                this.SummitSystem.DataReceivedFFTHandler -= value;
            }
        }

        public event EventHandler<SensingEventPower> DataReceivedPowerHandler
        {
            add
            {
                this.SummitSystem.DataReceivedPowerHandler += value;
            }

            remove
            {
                this.SummitSystem.DataReceivedPowerHandler -= value;
            }
        }

        public event EventHandler<SensingEventAccel> DataReceivedAccelHandler
        {
            add
            {
                this.SummitSystem.DataReceivedAccelHandler += value;
            }

            remove
            {
                this.SummitSystem.DataReceivedAccelHandler -= value;
            }
        }

        public event EventHandler<AdaptiveDetectEvent> DataReceivedDetectorHandler
        {
            add
            {
                this.SummitSystem.DataReceivedDetectorHandler += value;
            }

            remove
            {
                this.SummitSystem.DataReceivedDetectorHandler -= value;
            }
        }

        public event EventHandler<LoopRecordUpdateEvent> DataReceivedLoopRecordUpdateHandler
        {
            add
            {
                this.SummitSystem.DataReceivedLoopRecordUpdateHandler += value;
            }

            remove
            {
                this.SummitSystem.DataReceivedLoopRecordUpdateHandler -= value;
            }
        }

        public event EventHandler<ExternalMarkerEchoEvent> DataReceivedMarkerEchoHandler
        {
            add
            {
                this.SummitSystem.DataReceivedMarkerEchoHandler += value;
            }

            remove
            {
                this.SummitSystem.DataReceivedMarkerEchoHandler -= value;
            }
        }

        /// <summary>
        /// Gets or sets the SummitSystem to proxy calls to.
        /// </summary>
        public SummitSystem SummitSystem { get; set; }

        public bool IsDisposed
        {
            get
            {
                return this.SummitSystem.IsDisposed;
            }
        }

        public APIReturnInfo ReadTelemetryModuleInfo(out TelemetryModuleInfo theTelemetryInfo)
        {
            return this.SummitSystem.ReadTelemetryModuleInfo(out theTelemetryInfo);
        }

        public APIReturnInfo OlympusDiscovery(out List<DiscoveredDevice> theDiscoveredDevices)
        {
            return this.SummitSystem.OlympusDiscovery(out theDiscoveredDevices);
        }

        public APIReturnInfo StartInsSession(DiscoveredDevice? theInsInfo, out ConnectReturn returnState, bool disableAnnotations = false)
        {
            return this.SummitSystem.StartInsSession(theInsInfo, out returnState, disableAnnotations);
        }

        public APIReturnInfo ReadBatteryLevel(out BatteryStatusResult theBatteryStatus)
        {
            return this.SummitSystem.ReadBatteryLevel(out theBatteryStatus);
        }

        public APIReturnInfo WriteSensingTimeDomainChannels(List<TimeDomainChannel> channels)
        {
            return this.SummitSystem.WriteSensingTimeDomainChannels(channels);
        }

        public APIReturnInfo WriteSensingFftSettings(FftConfiguration fftConfig)
        {
            return this.SummitSystem.WriteSensingFftSettings(fftConfig);
        }

        public APIReturnInfo WriteSensingPowerChannels(BandEnables enables, List<PowerChannel> channels)
        {
            return this.SummitSystem.WriteSensingPowerChannels(enables, channels);
        }

        public APIReturnInfo WriteSensingMiscSettings(MiscellaneousSensing miscSettings)
        {
            return this.SummitSystem.WriteSensingMiscSettings(miscSettings);
        }

        public APIReturnInfo WriteSensingAccelSettings(AccelSampleRate theSampleRate)
        {
            return this.SummitSystem.WriteSensingAccelSettings(theSampleRate);
        }

        public APIReturnInfo ReadGeneralInfo(out GeneralInterrogateData data)
        {
            return this.SummitSystem.ReadGeneralInfo(out data);
        }

        public APIReturnInfo LeadIntegrityTest(List<Tuple<byte, byte>> testElectrodes, out LeadIntegrityTestResult theTestResult)
        {
            return this.SummitSystem.LeadIntegrityTest(testElectrodes, out theTestResult);
        }

        public APIReturnInfo WriteSensingState(SenseStates theNewSensingState, SenseTimeDomainChannel theFftstreamChannel)
        {
            return this.SummitSystem.WriteSensingState(theNewSensingState, theFftstreamChannel);
        }

        public APIReturnInfo WriteSensingEnableStreams(bool streamTD, bool streamFFT, bool streamPWR, bool streamDetector, bool streamAdaptiveState, bool streamAccel, bool streamTime, bool streamLoopRecordMarkerEcho)
        {
            return this.SummitSystem.WriteSensingEnableStreams(streamTD, streamFFT, streamPWR, streamDetector, streamAdaptiveState, streamAccel, streamTime, streamLoopRecordMarkerEcho);
        }

        public APIReturnInfo WriteSensingDisableStreams(bool streamTD, bool streamFFT, bool streamPWR, bool streamDetector, bool streamAdaptiveState, bool streamAccel, bool streamTime, bool streamLoopRecordMarkerEcho)
        {
            return this.SummitSystem.WriteSensingDisableStreams(streamTD, streamFFT, streamPWR, streamDetector, streamAdaptiveState, streamAccel, streamTime, streamLoopRecordMarkerEcho);
        }

        public APIReturnInfo WriteSensingDisableStreams(bool v)
        {
            return this.SummitSystem.WriteSensingDisableStreams(v);
        }

        public APIReturnInfo WriteTelemetrySoundEnables(CtmBeepEnables soundsToEnable)
        {
            return this.SummitSystem.WriteTelemetrySoundEnables(soundsToEnable);
        }
#pragma warning restore SA1600 // Elements should be documented
    }
}

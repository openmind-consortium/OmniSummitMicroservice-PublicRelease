// <copyright file="ISummitSystem.cs" company="OpenMind">
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
    /// Interface for the SummitSystem methods.
    /// </summary>
    public interface ISummitSystem
    {
#pragma warning disable SA1600 // Elements should be documented
        event EventHandler UnexpectedCtmDisconnectHandler;

        event EventHandler<SensingEventTD> DataReceivedTDHandler;

        event EventHandler<SensingEventFFT> DataReceivedFFTHandler;

        event EventHandler<SensingEventPower> DataReceivedPowerHandler;

        event EventHandler<SensingEventAccel> DataReceivedAccelHandler;

        event EventHandler<AdaptiveDetectEvent> DataReceivedDetectorHandler;

        event EventHandler<LoopRecordUpdateEvent> DataReceivedLoopRecordUpdateHandler;

        event EventHandler<ExternalMarkerEchoEvent> DataReceivedMarkerEchoHandler;

        bool IsDisposed { get; }

        APIReturnInfo ReadTelemetryModuleInfo(out TelemetryModuleInfo theTelemetryInfo);

        APIReturnInfo OlympusDiscovery(out List<DiscoveredDevice> theDiscoveredDevices);

        APIReturnInfo StartInsSession(DiscoveredDevice? theInsInfo, out ConnectReturn returnState, bool disableAnnotations = false);

        APIReturnInfo ReadBatteryLevel(out BatteryStatusResult theBatteryStatus);

        APIReturnInfo WriteSensingTimeDomainChannels(List<TimeDomainChannel> channels);

        APIReturnInfo WriteSensingFftSettings(FftConfiguration fftConfig);

        APIReturnInfo WriteSensingPowerChannels(BandEnables enables, List<PowerChannel> channels);

        APIReturnInfo WriteSensingMiscSettings(MiscellaneousSensing miscSettings);

        APIReturnInfo WriteSensingAccelSettings(AccelSampleRate theSampleRate);

        APIReturnInfo ReadGeneralInfo(out GeneralInterrogateData data);

        APIReturnInfo LeadIntegrityTest(List<Tuple<byte, byte>> testElectrodes, out LeadIntegrityTestResult theTestResult);

        APIReturnInfo WriteSensingState(SenseStates theNewSensingState, SenseTimeDomainChannel theFftstreamChannel);

        APIReturnInfo WriteSensingEnableStreams(bool streamTD, bool streamFFT, bool streamPWR, bool streamDetector, bool streamAdaptiveState, bool streamAccel, bool streamTime, bool streamLoopRecordMarkerEcho);

        APIReturnInfo WriteSensingDisableStreams(bool streamTD, bool streamFFT, bool streamPWR, bool streamDetector, bool streamAdaptiveState, bool streamAccel, bool streamTime, bool streamLoopRecordMarkerEcho);

        APIReturnInfo WriteSensingDisableStreams(bool v);

        APIReturnInfo WriteTelemetrySoundEnables(CtmBeepEnables soundsToEnable);
#pragma warning restore SA1600 // Elements should be documented
    }
}

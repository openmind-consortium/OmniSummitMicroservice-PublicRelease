// <copyright file="ISummitManager.cs" company="OpenMind">
// Copyright (c) OpenMind. All rights reserved.
// </copyright>

namespace OpenMindServer.Wrappers
{
    using System;
    using System.Collections.Generic;
    using Medtronic.SummitAPI.Classes;
    using Medtronic.TelemetryM;
    using Medtronic.TelemetryM.CtmProtocol.Commands;

    /// <summary>
    /// Interface for the SummitManager methods.
    /// </summary>
    public interface ISummitManager : IDisposable
    {
#pragma warning disable SA1600 // Elements should be documented
        ManagerConnectStatus CreateSummit(out ISummitSystem summitSystem, InstrumentInfo theTelemetry, InstrumentPhysicalLayers connectionType = InstrumentPhysicalLayers.Any, ushort telemetryMode = 3, byte telemetryRatio = 4, CtmBeepEnables ctmBeepEnables = CtmBeepEnables.DeviceDiscovered);

        List<InstrumentInfo> GetKnownTelemetry();

        List<InstrumentInfo> GetConnectedTelemetry();

        List<InstrumentInfo> GetUsbTelemetry();

        void DisposeSummit(ISummitSystem aSummitSystem);
#pragma warning restore SA1600 // Elements should be documented
    }
}

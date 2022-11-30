// <copyright file="OmniManager.cs" company="OpenMind">
// Copyright (c) OpenMind. All rights reserved.
// </copyright>

namespace OpenMindServer.Wrappers
{
    using System.Collections.Generic;
    using Medtronic.SummitAPI.Classes;
    using Medtronic.TelemetryM;
    using Medtronic.TelemetryM.CtmProtocol.Commands;

    /// <summary>
    /// Proxy for SummitManager. Implements the ISummitManager interface which helps with testing.
    /// </summary>
    public class OmniManager : ISummitManager
    {
        /// <summary>
        /// The SummitManager to proxy calls to.
        /// </summary>
        private readonly SummitManager summitManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="OmniManager"/> class.
        /// </summary>
        /// <param name="projectName">Project name for the SummitManager.</param>
        /// <param name="streamQueueSize">Stream queue size for the SummitManager.</param>
        /// <param name="verboseTraceLogging">Verbose trace logging enabled.</param>
        public OmniManager(string projectName = "OMNI Summit Device Service", int streamQueueSize = 200, bool verboseTraceLogging = false)
        {
            this.summitManager = new SummitManager(projectName, streamQueueSize, verboseTraceLogging);
        }

// NOTE (BNR): We don't document this stuff because it's just wrapping the SummitSystem
//             which already has ample documentation. Perhaps there's a way to link
//             documentation between this class and SummitSystem?
#pragma warning disable SA1600 // Elements should be documented
        public void Dispose()
        {
            this.summitManager.Dispose();
        }

        public ManagerConnectStatus CreateSummit(out ISummitSystem theOmniSystem, InstrumentInfo theTelemetry, InstrumentPhysicalLayers connectionType = InstrumentPhysicalLayers.Any, ushort telemetryMode = 3, byte telemetryRatio = 4, CtmBeepEnables ctmBeepEnables = CtmBeepEnables.DeviceDiscovered)
        {
            var status = this.summitManager.CreateSummit(out var summitSystem, theTelemetry, connectionType, telemetryMode, telemetryRatio, ctmBeepEnables);
            theOmniSystem = new OmniSystem(summitSystem);
            return status;
        }

        public List<InstrumentInfo> GetKnownTelemetry()
        {
            return this.summitManager.GetKnownTelemetry();
        }

        public List<InstrumentInfo> GetConnectedTelemetry()
        {
            return this.summitManager.GetConnectedTelemetry();
        }

        public List<InstrumentInfo> GetUsbTelemetry()
        {
            return this.summitManager.GetUsbTelemetry();
        }

        public void DisposeSummit(ISummitSystem omniSystem)
        {
            if (omniSystem.GetType().Name == "OmniSystem")
            {
                var summitSystem = ((OmniSystem)omniSystem).SummitSystem;
                this.summitManager.DisposeSummit(summitSystem);
            }
        }
    }
}

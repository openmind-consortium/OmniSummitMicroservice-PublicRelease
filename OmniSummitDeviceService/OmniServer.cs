// <copyright file="OmniServer.cs" company="OpenMind">
// Copyright (c) OpenMind. All rights reserved.
// </copyright>

namespace OpenMindServer
{
    using System;
    using Grpc.Core;

    /// <summary>
    /// A server object for hosting an OMNI-summit service.
    /// </summary>
    public class OmniServer
    {
        // Private variables
        private readonly Repository theRepository;
        private SummitService theSummitService;
        private Grpc.Core.Server theServer;
        private string host = "localhost";
        private int port = 50051;
        private string supportedDevices = "Medtronic Summit RC+S";

        /// <summary>
        /// Initializes a new instance of the <see cref="OmniServer"/> class.
        /// </summary>
        /// <param name="aSummitService">The Summit service to provide gRPC access to.</param>
        /// <param name="aRepository">The repository to provide gRPC access to.</param>
        public OmniServer(Repository aRepository, SummitService aSummitService)
        {
            this.theRepository = aRepository;
            this.theSummitService = aSummitService;

            if (System.Environment.GetEnvironmentVariable("OMNI_HOST") != null)
            {
                this.host = System.Environment.GetEnvironmentVariable("OMNI_HOST");
            }

            if (System.Environment.GetEnvironmentVariable("OMNI_PORT") != null)
            {
                this.port = Convert.ToInt32(System.Environment.GetEnvironmentVariable("OMNI_PORT"));
            }
        }

        /// <summary>
        /// Gets the Port number of the gRPC server.
        /// </summary>
        public int Port
        {
            get { return this.port; }
        }

        /// <summary>
        /// Gets the host string of the gRPC server.
        /// </summary>
        public string Host
        {
            get { return this.host; }
        }

        /// <summary>
        /// Gets the supported devices string.
        /// </summary>
        public string SupportedDevices
        {
            get { return this.supportedDevices; }
        }

        /// <summary>
        /// Starts the server using the constructor provided service.
        /// </summary>
        public void StartServer()
        {
            this.theServer = new Grpc.Core.Server
            {
                Services =
                {
                    OpenMind.BridgeManagerService.BindService(new Services.BridgeManagerService(this.theSummitService)),
                    OpenMind.DeviceManagerService.BindService(new Services.DeviceManagerService(this.theSummitService)),
                    OpenMind.InfoService.BindService(new Services.InfoService(this.theRepository, this.supportedDevices)),
                },

                /**
                 * TODO (BNR): Make the server port configurable. Add optional server credentials too.
                 */
                Ports = { new ServerPort(this.host, this.port, ServerCredentials.Insecure) },
            };
            this.theServer.Start();
        }

        /// <summary>
        /// Initiates a shutdown of the server, waits on all pending calls being completed.
        /// </summary>
        public void StopServer()
        {
            this.theSummitService.Shutdown();
            this.theServer.ShutdownAsync().Wait();
        }
    }
}

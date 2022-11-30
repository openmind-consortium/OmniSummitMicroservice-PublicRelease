// <copyright file="Server.cs" company="OpenMind">
// Copyright (c) OpenMind. All rights reserved.
// </copyright>

namespace OpenMindServer
{
    using System;
    using Grpc.Core;
    using OpenMindServer.Wrappers;

    /// <summary>
    /// The gRPC server.
    /// </summary>
    public class Server
    {
        /// <summary>
        /// Entrypoint of the server.
        /// </summary>
        /// <param name="args">Command line arguments for the server. Unused at the moment.</param>
        public static void Main(string[] args)
        {
            var repository = Repository.GetRepositoryInstance();
            var omniManager = new OmniManager();
            var summitService = new SummitService(repository, omniManager);

            OmniServer theServerInstance = new OmniServer(repository, summitService);
            theServerInstance.StartServer();

            Console.WriteLine($"OpenMindServer started on port {theServerInstance.Host}:{theServerInstance.Port}");
            Console.WriteLine($"Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()}");
            Console.WriteLine($"Supported devices: {theServerInstance.SupportedDevices}");
            Console.WriteLine("Press 'q' key to stop server...");
            var key = Console.ReadKey().Key;
            while (key != ConsoleKey.Q)
            {
                key = Console.ReadKey().Key;
            }

            theServerInstance.StopServer();
        }
    }
}

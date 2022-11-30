// <copyright file="InfoService.cs" company="OpenMind">
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
    public class InfoService : OpenMind.InfoService.InfoServiceBase
    {
        /// <summary>
        /// The repository used to translate calls between gRPC and Repository.
        /// </summary>
        private readonly Repository repository;

        /// <summary>
        /// The supported devices string.
        /// </summary>
        private readonly string supportedDevices;

        /// <summary>
        /// Initializes a new instance of the <see cref="InfoService"/> class.
        /// </summary>
        /// <param name="repository">Persistance layer for Summit connections.</param>
        public InfoService(Repository repository, string supportedDevices)
        {
            this.repository = repository;
            this.supportedDevices = supportedDevices;
        }

        /// <summary>
        /// Inspect repository endpoint.
        /// </summary>
        /// <param name="request">Inspect repository request.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Inspect repository response.</returns>
        public override Task<InspectRepositoryResponse> InspectRepository(InspectRepositoryRequest request, ServerCallContext context)
        {
            var response = new InspectRepositoryResponse { };
            response.RepoUri.AddRange(this.repository.GetAddresses());
            return Task.FromResult(response);
        }

        /// <summary>
        /// Supported Devices endpoint.
        /// </summary>
        /// <param name="request">Supported Devices request.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Supported Devices response.</returns>
        public override Task<SupportedDevicesResponse> SupportedDevices(SupportedDevicesRequest request, ServerCallContext context)
        {
            var response = new SupportedDevicesResponse { };
            response.SupportedDevices.Add(this.supportedDevices);
            return Task.FromResult(response);
        }

        /// <summary>
        /// Version Number endpoint.
        /// </summary>
        /// <param name="request">Version Number request.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Version Number response.</returns>
        public override Task<VersionNumberResponse> VersionNumber(VersionNumberRequest request, ServerCallContext context)
        {
            var response = new VersionNumberResponse { };
            var versionNumberString = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            response.VersionNumber = versionNumberString;
            return Task.FromResult(response);
        }
    }
}

// <copyright file="BridgeManagerService.cs" company="OpenMind">
// Copyright (c) OpenMind. All rights reserved.
// </copyright>

namespace OpenMindServer.Services
{
    using System.Threading.Tasks;
    using Google.Protobuf.WellKnownTypes;
    using Grpc.Core;
    using OpenMind;

    /// <summary>
    /// The gRPC Bridge API.
    /// </summary>
    public class BridgeManagerService : OpenMind.BridgeManagerService.BridgeManagerServiceBase
    {
        private readonly SummitService summitService;

        /// <summary>
        /// Initializes a new instance of the <see cref="BridgeManagerService"/> class.
        /// </summary>
        /// <param name="summitService">gRPC to Summit translation service.</param>
        public BridgeManagerService(SummitService summitService)
        {
            this.summitService = summitService;
        }

        /// <summary>
        /// List bridges endpoint.
        /// </summary>
        /// <param name="request">List bridges request.</param>
        /// <param name="context">The call context.</param>
        /// <returns>A list of bridges based on request.</returns>
        public override Task<QueryBridgesResponse> ListBridges(QueryBridgesRequest request, ServerCallContext context)
        {
            return Task.FromResult(this.summitService.ListBridges(request));
        }

        /// <summary>
        /// Connected bridges endpoint.
        /// </summary>
        /// <param name="request">Connected bridges request.</param>
        /// <param name="context">The call context.</param>
        /// <returns>A list of the connected bridges based on request.</returns>
        public override Task<QueryBridgesResponse> ConnectedBridges(QueryBridgesRequest request, ServerCallContext context)
        {
            return Task.FromResult(this.summitService.ConnectedBridges(request));
        }

        /// <summary>
        /// Connect bridge endpoint.
        /// </summary>
        /// <param name="request">Connect bridge request.</param>
        /// <param name="context">The call context.</param>
        /// <returns>The connection status of the bridge connect attempt.</returns>
        public override Task<ConnectBridgeResponse> ConnectBridge(ConnectBridgeRequest request, ServerCallContext context)
        {
            return Task.FromResult(this.summitService.ConnectToBridge(request));
        }

        /// <summary>
        /// Connection status stream endpoint.
        /// </summary>
        /// <param name="request">Enable stream request.</param>
        /// <param name="responseStream">The event stream.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Async task.</returns>
        public override Task ConnectionStatusStream(SetStreamEnable request, IServerStreamWriter<ConnectionUpdate> responseStream, ServerCallContext context)
        {
            return this.summitService.CreateBridgeConnectionStream(request, responseStream);
        }

        /// <summary>
        /// Disconnect bridge endpoint.
        /// </summary>
        /// <param name="request">Disconnect bridge request.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Nothing.</returns>
        public override Task<Empty> DisconnectBridge(DisconnectBridgeRequest request, ServerCallContext context)
        {
            return Task.FromResult(this.summitService.DisconnectFromBridge(request));
        }

        /// <summary>
        /// Describes a requested bridge.
        /// </summary>
        /// <param name="request">The bridge to query for descriptive information.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Descriptive information about the requested bridge.</returns>
        public override Task<DescribeBridgeResponse> DescribeBridge(DescribeBridgeRequest request, ServerCallContext context)
        {
            return Task.FromResult(this.summitService.DescribeBridge(request));
        }

        /// <summary>
        /// Configures beep endpoint.
        /// </summary>
        /// <param name="request">Configure beep request.</param>
        /// <param name="context">The call context.</param>
        /// <returns>Configure beep response.</returns>
        public override Task<ConfigureBeepResponse> ConfigureBeep(ConfigureBeepRequest request, ServerCallContext context)
        {
            return Task.FromResult(this.summitService.ConfigureBeep(request));
        }
    }
}

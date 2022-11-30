using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Medtronic.TelemetryM;
using Moq;
using NUnit.Framework;
using OpenMind;
using OpenMindServer;
using OpenMindServer.Wrappers;
using Medtronic.NeuroStim.Olympus.DataTypes.Measurement;
using Medtronic.NeuroStim.Olympus.DataTypes.PowerManagement;
using Medtronic.SummitAPI.Classes;
using Medtronic.NeuroStim.Olympus.DataTypes.Sensing;
using System.Runtime.Serialization;

namespace OpenMindServer.UnitTests
{
    [TestFixture]
    public partial class SummitServiceTest
    {
        // Tests Requirement 5.2.2 - Bridge Management
        // Tests Requirement 8.4.1 - Use localhost
        [Test]
        public void ListBridges_WithDuplicateBridges_ReturnsUnique()
        {
            var mock = new Mock<ISummitManager>();
            var knownTelemetry = new List<InstrumentInfo>();
            var usbTelemetry = new List<InstrumentInfo>();

            knownTelemetry.Add(new InstrumentInfo("foo", "foo", "foo", "foo", Models.Ctm2, DateTime.Now));
            knownTelemetry.Add(new InstrumentInfo("bar", "bar", "bar", "bar", Models.Ctm2, DateTime.Now));
            mock.Setup(SummitManager => SummitManager.GetKnownTelemetry())
                .Returns(knownTelemetry);

            usbTelemetry.Add(new InstrumentInfo("foo", "foo", "foo", "foo", Models.Ctm2, DateTime.Now));
            usbTelemetry.Add(new InstrumentInfo("bar", "bar", "bar", "bar", Models.Ctm2, DateTime.Now));
            usbTelemetry.Add(new InstrumentInfo("baz", "baz", "baz", "baz", Models.Ctm2, DateTime.Now));
            mock.Setup(SummitManager => SummitManager.GetUsbTelemetry())
                .Returns(usbTelemetry);

            var repository = Repository.GetRepositoryInstance();
            var summitService = new SummitService(repository, mock.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new BridgeManagerService.BridgeManagerServiceClient(channel);
            var bridges = aBridgeManagerClient.ListBridges(new QueryBridgesRequest());

            string[] expectedBridgeNames = { "//summit/bridge/foo", "//summit/bridge/bar", "//summit/bridge/baz" };
            Assert.That(bridges.Bridges.Count == expectedBridgeNames.Length);
            Assert.That(() => bridges.Bridges.All((bridge) => expectedBridgeNames.Contains(bridge.Name)));

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.2 - Bridge Management
        [Test]
        public void ListBridges_WithQuery_ReturnsFiltered()
        {
            var mock = new Mock<ISummitManager>();
            var knownTelemetry = new List<InstrumentInfo>();
            var usbTelemetry = new List<InstrumentInfo>();

            knownTelemetry.Add(new InstrumentInfo("foo", "foo", "foo", "foo", Models.Ctm2, DateTime.Now));
            knownTelemetry.Add(new InstrumentInfo("bar", "bar", "bar", "bar", Models.Ctm2, DateTime.Now));
            mock.Setup(SummitManager => SummitManager.GetKnownTelemetry())
                .Returns(knownTelemetry);

            usbTelemetry.Add(new InstrumentInfo("foo", "foo", "foo", "foo", Models.Ctm2, DateTime.Now));
            usbTelemetry.Add(new InstrumentInfo("bar", "bar", "bar", "bar", Models.Ctm2, DateTime.Now));
            usbTelemetry.Add(new InstrumentInfo("baz", "baz", "baz", "baz", Models.Ctm2, DateTime.Now));
            mock.Setup(SummitManager => SummitManager.GetUsbTelemetry())
                .Returns(usbTelemetry);

            var repository = Repository.GetRepositoryInstance();
            var summitService = new SummitService(repository, mock.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new BridgeManagerService.BridgeManagerServiceClient(channel);
            var bridges = aBridgeManagerClient.ListBridges(new QueryBridgesRequest
            {
                Query = "//summit/bridge/b"
            });

            string[] expectedBridgeNames = { "//summit/bridge/bar", "//summit/bridge/baz" };
            Assert.That(bridges.Bridges.Count == expectedBridgeNames.Length);
            Assert.That(() => bridges.Bridges.All((bridge) => expectedBridgeNames.Contains(bridge.Name)));

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.2 - Bridge Management
        [Test]
        public void ListBridges_WithNoBridges_ReturnsNoBridges()
        {
            var mock = new Mock<ISummitManager>();
            var knownTelemetry = new List<InstrumentInfo>();
            var usbTelemetry = new List<InstrumentInfo>();

            mock.Setup(SummitManager => SummitManager.GetKnownTelemetry())
                .Returns(knownTelemetry);
            mock.Setup(SummitManager => SummitManager.GetUsbTelemetry())
                .Returns(usbTelemetry);

            var repository = Repository.GetRepositoryInstance();
            var summitService = new SummitService(repository, mock.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new BridgeManagerService.BridgeManagerServiceClient(channel);
            var bridges = aBridgeManagerClient.ListBridges(new QueryBridgesRequest());

            Assert.That(bridges.Bridges.Count == 0);

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.1.1 - Initialization of Services
        // Tests Requirement 5.2.2 - Bridge Management
        [Test]
        public void ConnectedBridges_WithBridges_ReturnsBridges()
        {
            var mock = new Mock<ISummitManager>();
            var connectedTelemetry = new List<InstrumentInfo>();
            connectedTelemetry.Add(new InstrumentInfo("foo", "foo", "foo", "foo", Models.Ctm2, DateTime.Now));
            connectedTelemetry.Add(new InstrumentInfo("bar", "bar", "bar", "bar", Models.Ctm2, DateTime.Now));
            mock.Setup(SummitManager => SummitManager.GetConnectedTelemetry())
                .Returns(connectedTelemetry);

            var repository = Repository.GetRepositoryInstance();
            var summitService = new SummitService(repository, mock.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new BridgeManagerService.BridgeManagerServiceClient(channel);
            var bridges = aBridgeManagerClient.ConnectedBridges(new QueryBridgesRequest());

            string[] expectedBridgeNames = { "//summit/bridge/foo", "//summit/bridge/bar" };
            Assert.That(bridges.Bridges.Count == expectedBridgeNames.Length);
            Assert.That(() => bridges.Bridges.All((bridge) => expectedBridgeNames.Contains(bridge.Name)));

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.2 - Bridge Management
        [Test]
        public void ConnectedBridges_WithQuery_ReturnsFiltered()
        {
            var mock = new Mock<ISummitManager>();
            var connectedTelemetry = new List<InstrumentInfo>();
            connectedTelemetry.Add(new InstrumentInfo("foo", "foo", "foo", "foo", Models.Ctm2, DateTime.Now));
            connectedTelemetry.Add(new InstrumentInfo("bar", "bar", "bar", "bar", Models.Ctm2, DateTime.Now));
            mock.Setup(SummitManager => SummitManager.GetConnectedTelemetry())
                .Returns(connectedTelemetry);

            var repository = Repository.GetRepositoryInstance();
            var summitService = new SummitService(repository, mock.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new BridgeManagerService.BridgeManagerServiceClient(channel);
            var bridges = aBridgeManagerClient.ConnectedBridges(new QueryBridgesRequest
            {
                Query = "//summit/bridge/b"
            });

            string[] expectedBridgeNames = { "//summit/bridge/bar" };
            Assert.That(bridges.Bridges.Count == expectedBridgeNames.Length);
            Assert.That(() => bridges.Bridges.All((bridge) => expectedBridgeNames.Contains(bridge.Name)));

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.2 - Bridge Management
        [Test]
        public void ConnectedBridges_WithNoBridges_ReturnsNoBridges()
        {
            var mock = new Mock<ISummitManager>();
            var connectedTelemetry = new List<InstrumentInfo>();
            mock.Setup(SummitManager => SummitManager.GetConnectedTelemetry())
                .Returns(connectedTelemetry);

            var repository = Repository.GetRepositoryInstance();
            var summitService = new SummitService(repository, mock.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new BridgeManagerService.BridgeManagerServiceClient(channel);

            var bridges = aBridgeManagerClient.ConnectedBridges(new QueryBridgesRequest());

            // Close out the server
            aTestServer.StopServer();

            Assert.That(bridges.Bridges.Count == 0);

        }

        // Tests Requirement 5.2.3 - Confirms that when the Summit is in the repo and disposed that the connection call still proceeds
        [Test]
        public void ConnectToBridge_CachedConnectionIsDisposed_ReturnsSuccess()
        {
            // Construct mock system 
            Mock<ISummitManager> mockManager = new Mock<ISummitManager>();
            Mock<ISummitSystem> mockSystem = new Mock<ISummitSystem>();
            Repository repository = Repository.GetRepositoryInstance();

            // Cache bridge connection
            DateTime theTime = DateTime.Now;
            InstrumentInfo ourInstrument = new InstrumentInfo("usbAddress", "wirelessAddress", "serNum", "encrypt", Models.Ctm2, theTime);
            // SummitSystem ourSystem = new SummitSystem();
            repository.CacheConnection("//summit/bridge/foo", mockSystem.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            repository.CacheBridgeAddress("//summit/bridge/foo", ourInstrument);
// 
            // mockSystem setup to show system is disposed
            mockSystem.Setup(m => m.IsDisposed)
                .Returns(true)
                .Verifiable();

            // Set up mock CreateSummit call 
            ISummitSystem nullSystem = mockSystem.Object;
            mockManager.Setup(m => m.CreateSummit(out nullSystem, ourInstrument, InstrumentPhysicalLayers.Any, (ushort)4, (byte)1, Medtronic.TelemetryM.CtmProtocol.Commands.CtmBeepEnables.TelMLost))
            .Returns(ManagerConnectStatus.Success)
            .Verifiable();

            // Construct Summit Service
            SummitService summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test and start the server
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            //ConnectBridgeResponse response = aBridgeManagerClient.ConnectBridge(new ConnectBridgeRequest
            ConnectBridgeResponse response = summitService.ConnectToBridge(new ConnectBridgeRequest
            {
                Name = "//summit/bridge/foo",
                TelemetryMode = "4",
                TelemetryRatio = 1,
                PhysicalLayer = new SummitPhysicalLayer(),
                BeepEnables = SummitBeepConfig.TelMLost,
                Retries = 1
            });

            // Stops Server before assert to prevent server from persisting 
            aTestServer.StopServer();

            // Verifies that setup functions were called 
            mockSystem.Verify(m => m.IsDisposed);
            mockManager.Verify(m => m.CreateSummit(out nullSystem, ourInstrument, InstrumentPhysicalLayers.Any, (ushort)4, (byte)1, Medtronic.TelemetryM.CtmProtocol.Commands.CtmBeepEnables.TelMLost));

            // Checks that our actual output matches our expected
            Assert.AreEqual(response.ConnectionStatus, SummitConnectBridgeStatus.ConnectBridgeSuccess);
        }

        // Tests Requirement 5.2.3 - Confirms that Summit connection failures are returned to the application
        [Test]
        public void ConnectToBridge_CachedConnectionNotDisposed_ReturnsSuccess()
        {
            // Construct mock system 
            Mock<ISummitManager> mockManager = new Mock<ISummitManager>();
            Mock<ISummitSystem> mockSystem = new Mock<ISummitSystem>();
            Repository repository = Repository.GetRepositoryInstance();

            // Cache bridge connection
            repository.CacheConnection("//summit/bridge/foo", mockSystem.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            // Construct Summit Service
            SummitService summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test and start the server
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // ConnectBridgeResponse response = aBridgeManagerClient.ConnectBridge(new ConnectBridgeRequest
            ConnectBridgeResponse response = summitService.ConnectToBridge(new ConnectBridgeRequest
            {
                Name = "//summit/bridge/foo",
                TelemetryMode = "4",
                TelemetryRatio = 1,
                PhysicalLayer = new SummitPhysicalLayer(),
                BeepEnables = SummitBeepConfig.TelMLost,
                Retries = 1
            });

            // Stops Server before assert to prevent server from persisting 
            aTestServer.StopServer();

            // Checks that our actual output matches our expected
            Assert.AreEqual(SummitConnectBridgeStatus.ConnectBridgeSuccess, response.ConnectionStatus);
        }

        // Tests Requirement 5.2.3 - Confirms that when the Summit is in the repo and disposed that the connection call still proceeds
        [Test]
        public void ConnectToBridge_NoCachedConnectionConnectFails_ReturnsError()
        {
            // Construct mock system 
            Mock<ISummitManager> mockManager = new Mock<ISummitManager>();
            Mock<ISummitSystem> mockSystem = new Mock<ISummitSystem>();
            Repository repository = Repository.GetRepositoryInstance();

            // Cache bridge connection
            DateTime theTime = DateTime.Now;
            InstrumentInfo ourInstrument = new InstrumentInfo("usbAddress", "wirelessAddress", "serNum", "encrypt", Models.Ctm2, theTime);
            repository.CacheBridgeAddress("//summit/bridge/foo", ourInstrument);

            // Construct Summit Service
            SummitService summitService = new SummitService(repository, mockManager.Object);

            // Set up mock CreateSummit call 
            ISummitSystem nullSystem;
            mockManager.Setup(m => m.CreateSummit(out nullSystem, ourInstrument, InstrumentPhysicalLayers.Any, (ushort)4, (byte)1, Medtronic.TelemetryM.CtmProtocol.Commands.CtmBeepEnables.TelMLost))
            .Returns(ManagerConnectStatus.FailedTimedOut)
            .Verifiable();

            // Create the gRPC server to host the test and start the server
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Connect to Bridge with invalid ConnectBridgeRequest
            ConnectBridgeResponse response = summitService.ConnectToBridge(new ConnectBridgeRequest
            {
                Name = "//summit/bridge/foo",
                TelemetryMode = "4",
                TelemetryRatio = 1,
                PhysicalLayer = SummitPhysicalLayer.Any,
                BeepEnables = SummitBeepConfig.TelMLost,
                Retries = 1
            });

            // Stops Server before verifications and asserts to prevent server from persisting 
            aTestServer.StopServer();

            // Verifies that setup mock functions were called
            mockManager.VerifyAll();

            // Check that the output matches the expected output
            Assert.AreEqual(SummitConnectBridgeStatus.FailedTimeOut, response.ConnectionStatus);
        }

        // Tests Requirement 5.2.3 - Confirms that Summit connection successes are returned to the application
        [Test]
        public void ConnectToBridge_NoCachedConnectionRepoSuccess_ReturnsSuccess()
        {
            // Construct mock system 
            Mock<ISummitManager> mockManager = new Mock<ISummitManager>();
            Mock<ISummitSystem> mockSystem = new Mock<ISummitSystem>();
            Repository repository = Repository.GetRepositoryInstance();

            // Cache bridge connection
            DateTime theTime = DateTime.Now;
            InstrumentInfo ourInstrument = new InstrumentInfo("usbAddress", "wirelessAddress", "serNum", "encrypt", Models.Ctm2, theTime);
            repository.CacheBridgeAddress("//summit/bridge/foo", ourInstrument);

            // Construct Summit Service
            SummitService summitService = new SummitService(repository, mockManager.Object);

            // Create SummitSystem to use as an out parameter for mocked CreateSummit call
            ISummitSystem outSystem = (ISummitSystem) mockSystem.Object;

            // Set up mock CreateSummit call 
            mockManager.Setup(m => m.CreateSummit(out outSystem, ourInstrument, InstrumentPhysicalLayers.Any, (ushort)4, (byte)1, Medtronic.TelemetryM.CtmProtocol.Commands.CtmBeepEnables.TelMLost))
            .Returns(ManagerConnectStatus.Success)
            .Verifiable();

            // Create the gRPC server to host the test and start the server
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Connect to Bridge with invalid ConnectBridgeRequest
            ConnectBridgeResponse response = summitService.ConnectToBridge(new ConnectBridgeRequest
            {
                Name = "//summit/bridge/foo",
                TelemetryMode = "4",
                TelemetryRatio = 1,
                PhysicalLayer = SummitPhysicalLayer.Any,
                BeepEnables = SummitBeepConfig.TelMLost,
                Retries = 1
            });

            // Stops Server before verifications and asserts to prevent server from persisting 
            aTestServer.StopServer();

            // Verifies that setup mock functions were called
            mockManager.VerifyAll();

            // Check that the output matches the expected output
            Assert.AreEqual(SummitConnectBridgeStatus.ConnectBridgeSuccess, response.ConnectionStatus);
        }

        
        // Tests Requirement 5.2.3 - Confirms that an error is returned if an invalid bridge is requested to connect to.
        [Test]
        public void ConnectToBridge_NoDiscoveredBridgeRequest_ReturnsError()
        {
            // Construct mock system 
            Mock<ISummitManager> mockManager = new Mock<ISummitManager>();
            Mock<ISummitSystem> mockSystem = new Mock<ISummitSystem>();
            Repository repository = Repository.GetRepositoryInstance();

            // Cache bridge connection
            DateTime theTime = DateTime.Now;
            InstrumentInfo ourInstrument = new InstrumentInfo("usbAddress", "wirelessAddress", "serNum", "encrypt", Models.Ctm2, theTime);
            repository.CacheBridgeAddress("//summit/bridge/foo", ourInstrument);

            // Construct Summit Service
            SummitService summitService = new SummitService(repository, mockManager.Object);

            // Create SummitSystem to use as an out parameter for mocked CreateSummit call
            ISummitSystem outSystem = (ISummitSystem)mockSystem.Object;

            // Set up mock CreateSummit call 
            mockManager.Setup(m => m.CreateSummit(out outSystem, ourInstrument, InstrumentPhysicalLayers.Any, (ushort)4, (byte)1, Medtronic.TelemetryM.CtmProtocol.Commands.CtmBeepEnables.TelMLost))
            .Returns(ManagerConnectStatus.Unconnected)
            .Verifiable();

            // Create the gRPC server to host the test and start the server
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Connect to Bridge with invalid ConnectBridgeRequest
            ConnectBridgeResponse response = summitService.ConnectToBridge(new ConnectBridgeRequest
            {
                Name = "//summit/bridge/foo",
                TelemetryMode = "4",
                TelemetryRatio = 1,
                PhysicalLayer = SummitPhysicalLayer.Any,
                BeepEnables = SummitBeepConfig.TelMLost,
                Retries = 1
            });

            // Stops Server before verifications and asserts to prevent server from persisting 
            aTestServer.StopServer();

            // Verifies that setup mock functions were called
            mockManager.VerifyAll();

            // Check that the output matches the expected output
            Assert.AreEqual(SummitConnectBridgeStatus.Unconnected, response.ConnectionStatus);
        }
        // Tests Requirement 5.2.3 - Confirms that the DisposeSummit function is called using the mock verify function
        [Test]
        public void DisconnectFromBridge_DisposesSummit()
        {
            var repository = Repository.GetRepositoryInstance();
            var mockManager = new Mock<ISummitManager>();
            var mockSystem = new Mock<ISummitSystem>();
            repository.CacheConnection("//summit/bridge/foo", mockSystem.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            var summitService = new SummitService(repository, mockManager.Object);
            mockManager.Setup(m => m.DisposeSummit(mockSystem.Object));
            
            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new BridgeManagerService.BridgeManagerServiceClient(channel);
            
            var response = aBridgeManagerClient.DisconnectBridge(new DisconnectBridgeRequest { Name = "//summit/bridge/foo" });
            
            aTestServer.StopServer();
            Assert.AreEqual(response, new Google.Protobuf.WellKnownTypes.Empty());
            mockManager.Verify(m => m.Dispose(), Times.Once());
        }

        // Tests Requirement 5.1.3 - Server Shutdown
        [Test]
        public void Shutdown_DisposesManager()
        {
            var mock = new Mock<ISummitManager>();
            var repository = Repository.GetRepositoryInstance();
            var summitService = new SummitService(repository, mock.Object);
            summitService.Shutdown();
            mock.Verify(m => m.Dispose());
        }

        // Tests Requirement 5.2.6 - Query state of INS Devices
        [Test]
        public void ListDevices_WithNoDevices_ReturnsNoDevices()
        {
            // First, populate the repository with a mock SummitSystem
            var mockManager = new Mock<ISummitManager>();
            var mockSystem = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            repository.CacheConnection("//summit/bridge/foo", mockSystem.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            // The ListDevices endpoint calls OlympusDiscovery on the SummitSystem.
            // Let's mock that call. This test is for when there's no devices available.
            // The call to OlympusDiscovery needs to return an empty list of DiscoveredDevices.
            List<DiscoveredDevice> devices = null;
            mockSystem.Setup(m => m.OlympusDiscovery(out devices))
                .Returns(new Medtronic.SummitAPI.Classes.APIReturnInfo(
                    Medtronic.SummitAPI.Classes.APIRejectCodes.NoError,
                    Medtronic.NeuroStim.Olympus.Commands.CommandCodes.Unused00,
                    DateTime.Now,
                    DateTime.Now,
                    0
                ))
                .Verifiable();

            // Now we can call the ListDevices method to trigger our code.
            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            var listDeviceResponse = aDeviceManagerClient.ListDevices(new ListDeviceRequest
            {
                Query = "//summit/bridge/foo"
            });

            // Now we can check that the mock called the right 
            mockSystem.Verify();
            Assert.AreEqual(listDeviceResponse.Devices, new Google.Protobuf.Collections.RepeatedField<Device>());

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.6 - Query state of INS Devices
        [Test]
        public void ListDevices_WithSameDeviceConnectedAlready_ReturnsMedtronicError()
        {
            // First, populate the repository with a mock SummitSystem
            var mockManager = new Mock<ISummitManager>();
            var mockSystem = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            repository.CacheConnection("//summit/bridge/foo", mockSystem.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            // The ListDevices endpoint calls OlympusDiscovery on the SummitSystem.
            // Let's mock that call. This test is for when there's no devices available.
            // The call to OlympusDiscovery needs to return an empty list of DiscoveredDevices.
            List<DiscoveredDevice> devices = new List<DiscoveredDevice>();
            mockSystem.Setup(m => m.OlympusDiscovery(out devices))
                .Returns(new Medtronic.SummitAPI.Classes.APIReturnInfo(
                    Medtronic.SummitAPI.Classes.APIRejectCodes.InsAlreadyConnected,
                    Medtronic.NeuroStim.Olympus.Commands.CommandCodes.Unused00,
                    DateTime.Now,
                    DateTime.Now,
                    0
                ))
                .Verifiable();

            // Now we can call the ListDevices method to trigger our code.
            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            var listDeviceResponse = aDeviceManagerClient.ListDevices(new ListDeviceRequest
            {
                Query = "//summit/bridge/foo"
            });

            // Now we can check that the mock called the right 
            mockSystem.Verify();
            var details = listDeviceResponse.Error;
            Assert.AreEqual(details.RejectCode, SummitApiErrorCode.InsAlreadyConnected);

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.6 - Query state of INS Devices
        [Test]
        public void ListDevices_WithDeviceSuccess_ReturnsDeviceList()
        {
            // First, populate the repository with a mock SummitSystem
            var mockManager = new Mock<ISummitManager>();
            var mockSystem = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            repository.CacheConnection("//summit/bridge/foo", mockSystem.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            // The ListDevices endpoint calls OlympusDiscovery on the SummitSystem.
            // Let's mock that call. This test is for when there's no devices available.
            // The call to OlympusDiscovery needs to return an empty list of DiscoveredDevices.
            List<DiscoveredDevice> devices = new List<DiscoveredDevice>();
            devices.Add(new DiscoveredDevice { deviceSerial = "bar", telMId = "id", isProximalDevice = true, ephemeralKey = new byte[0] });
            repository.CacheDeviceAddress("//summit/bridge/foo/device/bar", devices[0]);

            mockSystem.Setup(m => m.OlympusDiscovery(out devices))
                .Returns(new Medtronic.SummitAPI.Classes.APIReturnInfo(
                    Medtronic.SummitAPI.Classes.APIRejectCodes.NoError,
                    Medtronic.NeuroStim.Olympus.Commands.CommandCodes.Unused00,
                    DateTime.Now,
                    DateTime.Now,
                    0
                ))
                .Verifiable();

            // Now we can call the ListDevices method to trigger our code.
            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            var listDeviceResponse = aDeviceManagerClient.ListDevices(new ListDeviceRequest
            {
                Query = "//summit/bridge/foo/device/bar"
            });

            // Now we can check that the mock called the right 
            mockSystem.Verify();
            string[] expectedName = { "//summit/bridge/foo/device/bar" };

            Assert.That(devices.Count == listDeviceResponse.Devices.Count);
            Assert.That(() => listDeviceResponse.Devices.All((device) => expectedName.Contains(device.Name)));

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.1.1 - Initialization of Services
        // Tests Requirement 5.1.2 - Active Mode
        // Tests Requirement 5.2.6 - Query state of INS Devices
        [Test]
        public void DeviceStatus_WithSuccessfulRead_ReturnsBatteryStatus()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            repository.CacheConnection("//summit/bridge/foo", mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            BatteryStatusResult batteryStatusSuccess = new BatteryStatusResult
            {
                BatteryLevelPercent = 95,
                BatteryVoltage = 3,
                BatterySOC = 1,
                EstimatedCapacity = 2,
                ManufacturedCapacity = 4,
                SOCUncertainty = 5,
                TherapyUnavailableSOC = 6,
                FullSOC = 7
            };

            mockDevice.Setup(Device => Device.ReadBatteryLevel(out batteryStatusSuccess))
                .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            //Device Status requires a Name with device included but only uses the bridge Name
            DeviceStatusRequest deviceStatusRequest = new DeviceStatusRequest();
            deviceStatusRequest.Name = "//summit/bridge/foo/device/foo";
            var response = aDeviceManagerClient.DeviceStatus(deviceStatusRequest);

            Assert.That(response.BatterySoc == batteryStatusSuccess.BatterySOC);
            Assert.That(response.EstimatedCapacity == batteryStatusSuccess.EstimatedCapacity);
            Assert.That(response.FullSoc == batteryStatusSuccess.FullSOC);
            Assert.That(response.ManufacturedCapacity == batteryStatusSuccess.ManufacturedCapacity);
            Assert.That(response.SocUncertainty == batteryStatusSuccess.SOCUncertainty);
            Assert.That(response.TherapyUnavailableSoc == batteryStatusSuccess.TherapyUnavailableSOC);
            Assert.That(response.BatteryLevelPercent == batteryStatusSuccess.BatteryLevelPercent);
            Assert.That(response.BatteryLevelVoltage == batteryStatusSuccess.BatteryVoltage);

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.1.2 - Active Mode
        // Tests Requirement 5.2.6 - Query state of INS Devices
        [Test]
        public void DeviceStatus_DeviceDoesNotExist_ReturnsError()
        {
            // Create the mock interface
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            // Device Status requires a Name with device included but only uses the bridge Name
            DeviceStatusRequest deviceStatusRequest = new DeviceStatusRequest();
            deviceStatusRequest.Name = "//summit/bridge/foo/device/foo";
            var response = aDeviceManagerClient.DeviceStatus(deviceStatusRequest);

            // Assert that an error should be returned
            Assert.That(response.Error.Message == "Device requested is not connected");
            Assert.That((int)response.Error.RejectCode == -1);

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.6 - Query state of INS Devices
        [Test]
        public void DeviceStatus_WithUnsuccessfulRead_ReturnsNullBatteryLevels()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            repository.CacheConnection("//summit/bridge/foo", mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            BatteryStatusResult batteryStatusSuccess = null;

            mockDevice.Setup(Device => Device.ReadBatteryLevel(out batteryStatusSuccess))
                .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);
            
            //Device Status requires a Name with device included but only uses the bridge Name
            DeviceStatusRequest deviceStatusRequest = new DeviceStatusRequest();
            deviceStatusRequest.Name = "//summit/bridge/foo/device/foo";
            var response = aDeviceManagerClient.DeviceStatus(deviceStatusRequest);

            Assert.That(response.BatterySoc == null);
            Assert.That(response.EstimatedCapacity == null);
            Assert.That(response.FullSoc == null);
            Assert.That(response.ManufacturedCapacity == null);
            Assert.That(response.SocUncertainty == null);
            Assert.That(response.TherapyUnavailableSoc == null);
            Assert.That(response.BatteryLevelPercent == null);
            Assert.That(response.BatteryLevelVoltage == null);
            Assert.That(response.BatteryLevelVoltage == null);
            Assert.That(response.BatteryLevelPercent == null);

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.7 - Confirms that Summit connection failures are returned to the application.
        [Test]
        public void ConnectToDevice_RequestedDeviceNotCached_ReturnsError()
        {
            // Construct mock system 
            Mock<ISummitManager> mockManager = new Mock<ISummitManager>();
            Mock<ISummitSystem> mockSystem = new Mock<ISummitSystem>();
            Repository repository = Repository.GetRepositoryInstance();

            // Cache bridge connection
            repository.CacheConnection("//summit/bridge/foo", mockSystem.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            //Cache device address
            List<DiscoveredDevice> devices = new List<DiscoveredDevice>();
            devices.Add(new DiscoveredDevice { deviceSerial = "bar", telMId = "id", isProximalDevice = true, ephemeralKey = new byte[0] });
            repository.CacheDeviceAddress("//summit/bridge/foo/device/bar", devices[0]);

            // Construct Summit Service
            SummitService summitService = new SummitService(repository, mockManager.Object);

            // Create setup for StartInsSession
            var deviceAddress = repository.GetDeviceAddressByName("//summit/bridge/foo/device/bar");
            var disableAnnotations = true;
            ConnectReturn connectReturn = ConnectReturn.CriticalError;
            mockSystem.Setup(m => m.StartInsSession(deviceAddress, out connectReturn, disableAnnotations))
                .Returns(new Medtronic.SummitAPI.Classes.APIReturnInfo(
                    Medtronic.SummitAPI.Classes.APIRejectCodes.NoInsConnected,
                    Medtronic.NeuroStim.Olympus.Commands.CommandCodes.Unused00,
                    DateTime.Now,
                    DateTime.Now,
                    0
                ))
                .Verifiable();

            // Create the gRPC server to host the test and start the server
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            //ConnectBridgeResponse response = aBridgeManagerClient.ConnectBridge(new ConnectBridgeRequest
            ConnectDeviceResponse response = summitService.ConnectToDevice(new ConnectDeviceRequest
            {
                Name = "//summit/bridge/foo/device/baz",
            });

            // Stops Server before assert to prevent server from persisting 
            aTestServer.StopServer();

            // Verify that setup functions were called
            mockSystem.Verify(m => m.StartInsSession(deviceAddress, out connectReturn, disableAnnotations), Times.Never());

            // Checks that our actual output matches our expected
            var expected_response = new ConnectDeviceResponse() { Error = new SummitError() { RejectCode = (SummitApiErrorCode)(-1), Message = "Requested Device not in Repostiory" } };
            Assert.AreEqual(response, expected_response);
        }

        // Tests Requirement 5.2.7 - ConnectToDevice
        [Test]
        public void ConnecteToDevice_NoDeviceSpecified_NullConnectSuccess()
        {
            // Construct mock system 
            Mock<ISummitManager> mockManager = new Mock<ISummitManager>();
            Mock<ISummitSystem> mockSystem = new Mock<ISummitSystem>();
            Repository repository = Repository.GetRepositoryInstance();

            // Cache bridge connection
            repository.CacheConnection("//summit/bridge/foo", mockSystem.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            // Construct Summit Service
            SummitService summitService = new SummitService(repository, mockManager.Object);

            // Create setup for StartInsSession
            var deviceAddress = repository.GetDeviceAddressByName("//summit/bridge/foo"); 
            var disableAnnotations = true;
            ConnectReturn connectReturn = ConnectReturn.Success;
            mockSystem.Setup(m => m.StartInsSession(deviceAddress, out connectReturn, disableAnnotations))
                .Returns(new Medtronic.SummitAPI.Classes.APIReturnInfo(
                    Medtronic.SummitAPI.Classes.APIRejectCodes.NoError,
                    Medtronic.NeuroStim.Olympus.Commands.CommandCodes.Unused00,
                    DateTime.Now,
                    DateTime.Now,
                    0
                ))
                .Verifiable();

            // Create the gRPC server to host the test and start the server
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            //ConnectBridgeResponse response = aBridgeManagerClient.ConnectBridge(new ConnectBridgeRequest
            ConnectDeviceResponse response = summitService.ConnectToDevice(new ConnectDeviceRequest
            {
                Name = "//summit/bridge/foo",
            });

            // Stops Server before assert to prevent server from persisting 
            aTestServer.StopServer();

            // Verify that setup functions were called
            mockSystem.Verify(m => m.StartInsSession(deviceAddress, out connectReturn, disableAnnotations));


            // Checks that our actual output matches our expected
            Assert.AreEqual(ConnectReturn.Success, (ConnectReturn)response.ConnectionStatus);
        }
        
        // Tests Requirement 5.2.7 - Confirms that Summit connection device connection status is returned to the application.
        [Test]
        public void ConnectToDevice_CachedDeviceConnect_ReturnsConnectStatus()
        {
            // Construct mock system 
            Mock<ISummitManager> mockManager = new Mock<ISummitManager>();
            Mock<ISummitSystem> mockSystem = new Mock<ISummitSystem>();
            Repository repository = Repository.GetRepositoryInstance();

            // Cache bridge connection

            repository.CacheConnection("//summit/bridge/foo", mockSystem.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            
            //Cache device address
            List<DiscoveredDevice> devices = new List<DiscoveredDevice>();
            devices.Add(new DiscoveredDevice { deviceSerial = "bar", telMId = "id", isProximalDevice = true, ephemeralKey = new byte[0] });
            repository.CacheDeviceAddress("//summit/bridge/foo/device/bar", devices[0]);

            // Construct Summit Service
            SummitService summitService = new SummitService(repository, mockManager.Object);

            // Create setup for StartInsSession
            var deviceAddress = repository.GetDeviceAddressByName("//summit/bridge/foo/device/bar");
            var disableAnnotations = true;
            ConnectReturn connectReturn = ConnectReturn.Success;
            mockSystem.Setup(m => m.StartInsSession(deviceAddress, out connectReturn, disableAnnotations))
                .Returns(new Medtronic.SummitAPI.Classes.APIReturnInfo(
                    Medtronic.SummitAPI.Classes.APIRejectCodes.NoError,
                    Medtronic.NeuroStim.Olympus.Commands.CommandCodes.Unused00,
                    DateTime.Now,
                    DateTime.Now,
                    0
                ))
                .Verifiable();

            // Create the gRPC server to host the test and start the server
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            //ConnectBridgeResponse response = aBridgeManagerClient.ConnectBridge(new ConnectBridgeRequest
            ConnectDeviceResponse response = summitService.ConnectToDevice(new ConnectDeviceRequest
            {
                Name = "//summit/bridge/foo/device/bar",
            });

            // Stops Server before assert to prevent server from persisting 
            aTestServer.StopServer();

            // Verify that setup functions were called
            mockSystem.Verify(m => m.StartInsSession(deviceAddress, out connectReturn, disableAnnotations));

            // Checks that our actual output matches our expected
            Assert.AreEqual(ConnectReturn.Success, (ConnectReturn)response.ConnectionStatus);
        }

        // Tests Requirement 5.2.7 - Confirms that an error is returned if GetConnectionByName returns null.
        [Test]
        public void ConnectToDevice_NoConnectedBridge_ReturnsError()
        {
            // Construct mock system 
            Mock<ISummitManager> mockManager = new Mock<ISummitManager>();
            Mock<ISummitSystem> mockSystem = new Mock<ISummitSystem>();
            Repository repository = Repository.GetRepositoryInstance();

            // Construct Summit Service
            SummitService summitService = new SummitService(repository, mockManager.Object);

            // Create setup for StartInsSession when performing a null connect (null deviceAddress)
            var disableAnnotations = true;
            DiscoveredDevice? deviceAddress = null;
            ConnectReturn connectReturn = ConnectReturn.CriticalError;
            mockSystem.Setup(m => m.StartInsSession(deviceAddress, out connectReturn, disableAnnotations))
                .Returns(new Medtronic.SummitAPI.Classes.APIReturnInfo(
                    Medtronic.SummitAPI.Classes.APIRejectCodes.NoInsConnected,
                    Medtronic.NeuroStim.Olympus.Commands.CommandCodes.Unused00,
                    DateTime.Now,
                    DateTime.Now,
                    0
                ))
                .Verifiable();

            // Create the gRPC server to host the test and start the server
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            //ConnectBridgeResponse response = aBridgeManagerClient.ConnectBridge(new ConnectBridgeRequest
            ConnectDeviceResponse response = summitService.ConnectToDevice(new ConnectDeviceRequest
            {
                Name = "//summit/bridge/foo/device/bar",
            });

            // Stops Server before assert to prevent server from persisting 
            aTestServer.StopServer();

            // Verify that StartInsSession never gets called 
            mockSystem.Verify(m => m.StartInsSession(deviceAddress, out connectReturn, disableAnnotations), Times.Never());

            // Checks that our actual output matches our expected
            var expected_response = new ConnectDeviceResponse() { Error = new SummitError() { RejectCode = (SummitApiErrorCode)(-1), Message = "Bridge requested is not connected" } };

            // Checks that our actual output matches our expected
            Assert.AreEqual(expected_response, response);
        }

        // Tests Requirement 5.2.7 - Confirms that DisconnectFromDevice disposes the Summit
        [Test]
        public void DisconnectFromDevice_DisposesSummit()
        {
            // Construct mock system 
            Mock<ISummitManager> mockManager = new Mock<ISummitManager>();
            Mock<ISummitSystem> mockSystem = new Mock<ISummitSystem>();
            Repository repository = Repository.GetRepositoryInstance();
            // Cache bridge connection
            repository.CacheConnection("//summit/bridge/foo", mockSystem.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());           
            // Construct Summit Service
            SummitService summitService = new SummitService(repository, mockManager.Object);
            // Create setup for DisposeSummit
            mockManager.Setup(m => m.DisposeSummit(mockSystem.Object)).Verifiable();
            // Create setup for StartInsSession
            var deviceAddress = repository.GetDeviceAddressByName("//summit/bridge/foo/device/bar");
            var disableAnnotations = true;
            ConnectReturn connectReturn = ConnectReturn.Success;
            mockSystem.Setup(m => m.StartInsSession(deviceAddress, out connectReturn, disableAnnotations))
                .Returns(new Medtronic.SummitAPI.Classes.APIReturnInfo(
                    Medtronic.SummitAPI.Classes.APIRejectCodes.NoError,
                    Medtronic.NeuroStim.Olympus.Commands.CommandCodes.Unused00,
                    DateTime.Now,
                    DateTime.Now,
                    0
                ))
                .Verifiable();

            // Create the gRPC server to host the test and start the server
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();
            // Disconnect from device
            var response = summitService.DisconnectFromDevice(new DisconnectDeviceRequest
            {
                Name = "//summit/bridge/foo/device/bar",
            });

            // Stops Server before assert to prevent server from persisting 
            aTestServer.StopServer();

            // Verifies that setup mock functions were called
            mockManager.VerifyAll();


            // Checks that our actual output matches our expected
            Assert.AreEqual(response, new Google.Protobuf.WellKnownTypes.Empty());
        }
        // Tests Requirement 5.1.2 - Active Mode
        // Tests Requirement 5.2.2 - Bridge Management
        [Test]
        public void DescribeBridge_ErrorFromAPI_ReturnsErrorEmptyDetails()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            TelemetryModuleInfo telemetryModuleInfo = null;

            mockDevice.Setup(Bridge => Bridge.ReadTelemetryModuleInfo(out telemetryModuleInfo))
                .Returns(new APIReturnInfo(APIRejectCodes.CtmUnexpectedDisconnect, 0, DateTime.Now, DateTime.Now, 0));

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new BridgeManagerService.BridgeManagerServiceClient(channel);

            var describeBridgeResponse = aBridgeManagerClient.DescribeBridge(new DescribeBridgeRequest
            {
                Name = "//summit/bridge/foo"
            });

            Assert.That(describeBridgeResponse.Name == name);
            Assert.That(describeBridgeResponse.BatteryLevel == 0);
            Assert.That(describeBridgeResponse.BatteryStatus == "");
            Assert.That(describeBridgeResponse.BeepEnables == 0);
            Assert.That(describeBridgeResponse.FirmwareVersion == 0);
            Assert.That(describeBridgeResponse.ModuleType == "");
            Assert.That(describeBridgeResponse.PhysicalLayer == 0);
            Assert.That(describeBridgeResponse.SerialNumber == "");
            Assert.That(describeBridgeResponse.TelemetryMode == "");
            Assert.That(describeBridgeResponse.TelemetryRatio == 0);
            Assert.That(describeBridgeResponse.WireType == "");
            var error = describeBridgeResponse.Error;
            Assert.AreEqual(SummitApiErrorCode.CtmUnexpectedDisconnect, error.RejectCode);

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.1.2 - Active Mode
        // Tests Requirement 5.2.2 - Bridge Management
        [Test]
        public void DescribeBridge_NoErrorFromAPI_ReturnsDetails()
        {
            // Create the mock interface
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            // Due to TelemetryModuleInfo having an internal constructor, we cannot set values but we can create an uninitialized object.
            Type telemetryInfoType = typeof(TelemetryModuleInfo);
            TelemetryModuleInfo theTelemetryInfo = (TelemetryModuleInfo)FormatterServices.GetUninitializedObject(telemetryInfoType);

            mockDevice.Setup(Bridge => Bridge.ReadTelemetryModuleInfo(out theTelemetryInfo))
                .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new BridgeManagerService.BridgeManagerServiceClient(channel);

            var describeBridgeResponse = aBridgeManagerClient.DescribeBridge(new DescribeBridgeRequest
            {
                Name = "//summit/bridge/foo"
            });

            // Assert that the Telemetry Module Info is the unitialized object created 
            Assert.That(describeBridgeResponse.Name == name);
            Assert.That(describeBridgeResponse.BatteryLevel == 0);
            Assert.That(describeBridgeResponse.BatteryStatus == "");
            Assert.That(describeBridgeResponse.BeepEnables == 0);
            Assert.That(describeBridgeResponse.FirmwareVersion == 0);
            Assert.That(describeBridgeResponse.ModuleType == "");
            Assert.That(describeBridgeResponse.PhysicalLayer == 0);
            Assert.That(describeBridgeResponse.SerialNumber == "");
            Assert.That(describeBridgeResponse.TelemetryMode == "");
            Assert.That(describeBridgeResponse.TelemetryRatio == 0);
            Assert.That(describeBridgeResponse.WireType == "");
            var error = describeBridgeResponse.Error;
            Assert.AreEqual(SummitApiErrorCode.NoError, error.RejectCode);

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.1.2 - Active Mode
        // Tests Requirement 5.2.2 - Bridge Management
        [Test]
        public void DescribeBridge_DoesNotExist_ReturnsError()
        {
            // Create the mock interface
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new BridgeManagerService.BridgeManagerServiceClient(channel);

            // Device Status requires a Name with device included but only uses the bridge Name
            DescribeBridgeRequest bridgeDescriptionRequest = new DescribeBridgeRequest();
            bridgeDescriptionRequest.Name = "//summit/bridge/foo";
            var response = aBridgeManagerClient.DescribeBridge(bridgeDescriptionRequest);

            // Assert that an error should be returned
            Assert.That(response.Error.Message == "Bridge requested is not connected");
            Assert.That((int)response.Error.RejectCode == -1);

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.6 - Query state of INS Devices
        [Test]
        public void LeadIntegrityTest_NoError_ReturnsLeadList()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            LeadIntegrityTestResult integrityTestResult = new LeadIntegrityTestResult { };
            List<LeadIntegrityPairResult> pairResults = new List<LeadIntegrityPairResult>();
            pairResults.Add(new LeadIntegrityPairResult { Impedance = 5000, Info = (LeadIntegrityInfoTypes)4, Voltage = 16, });
            integrityTestResult.PairResults = pairResults;
            var electrodetuple = new List<Tuple<byte, byte>> { };
            electrodetuple.Add(new Tuple<byte, byte>(0, 3));

            mockDevice.Setup(Device => Device.LeadIntegrityTest(electrodetuple, out integrityTestResult))
                .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            ImpedanceRequest ImpReq = new ImpedanceRequest();
            LeadList testleadlist = new LeadList();
            testleadlist.Lead1 = 1;
            testleadlist.Lead2 = 4;
            ImpReq.Name = name;
            ImpReq.LeadList.Add(testleadlist);

            var ImpRes = aDeviceManagerClient.LeadIntegrityTest(ImpReq);

            var expectedResult = new List<Tuple<double>> { new Tuple<double>(0) };

            Assert.That(ImpRes.Name == name);
            Assert.That(ImpRes.ImpedanceErrorCode == ImpedanceErrorCode.NoImpedanceError);

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.6 - Query state of INS Devices
        [Test]
        public void LeadIntegrityTest_SameLeadValues_ReturnsZero()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            LeadIntegrityTestResult integrityTestResult = new LeadIntegrityTestResult { };
            List<LeadIntegrityPairResult> pairResults = new List<LeadIntegrityPairResult>();
            pairResults.Add(new LeadIntegrityPairResult { Impedance = 5000, Info = (LeadIntegrityInfoTypes)4, Voltage = 16, });
            integrityTestResult.PairResults = pairResults;
            var electrodetuple = new List<Tuple<byte, byte>> { };
            electrodetuple.Add(new Tuple<byte, byte>(3, 3));

            mockDevice.Setup(Device => Device.LeadIntegrityTest(electrodetuple, out integrityTestResult))
                .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            ImpedanceRequest ImpReq = new ImpedanceRequest();
            LeadList testleadlist = new LeadList();
            testleadlist.Lead1 = 4;
            testleadlist.Lead2 = 4;
            ImpReq.Name = name;
            ImpReq.LeadList.Add(testleadlist);

            var ImpRes = aDeviceManagerClient.LeadIntegrityTest(ImpReq);

            var expectedResult = new List<Tuple<double>> { new Tuple<double>(0) };

            Assert.That(ImpRes.Name == name);
            Assert.That(ImpRes.ImpedanceErrorCode == ImpedanceErrorCode.NoImpedanceError);

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.6 - Query state of INS Devices
        [Test]
        public void LeadIntegrityTest_OutofRangeLeadValues_ReturnsError()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            LeadIntegrityTestResult integrityTestResult = new LeadIntegrityTestResult { };
            List<LeadIntegrityPairResult> pairResults = new List<LeadIntegrityPairResult>();
            pairResults.Add(new LeadIntegrityPairResult { Impedance = 5000, Info = (LeadIntegrityInfoTypes)4, Voltage = 16, });
            integrityTestResult.PairResults = pairResults;
            var electrodetuple = new List<Tuple<byte, byte>> { };
            electrodetuple.Add(new Tuple<byte, byte>(30, 3));

            mockDevice.Setup(Device => Device.LeadIntegrityTest(electrodetuple, out integrityTestResult))
                .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            ImpedanceRequest ImpReq = new ImpedanceRequest();
            LeadList testleadlist = new LeadList();
            testleadlist.Lead1 = 31;
            testleadlist.Lead2 = 4;
            ImpReq.Name = name;
            ImpReq.LeadList.Add(testleadlist);

            var ImpRes = aDeviceManagerClient.LeadIntegrityTest(ImpReq);

            var expectedResult = new List<Tuple<double>> { new Tuple<double>(0) };

            Assert.That(ImpRes.Name == name);
            Assert.That(ImpRes.ImpedanceErrorCode == ImpedanceErrorCode.OutOfRangeLeadPairValues);

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.4 - Configure Beep
        [Test]
        public void ConfigureBeep_ValidRequestParameter_ReturnsSuccess()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";

            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            var summitService = new SummitService(repository, mockManager.Object);
            mockDevice.Setup(Bridge => Bridge.WriteTelemetrySoundEnables(Medtronic.TelemetryM.CtmProtocol.Commands.CtmBeepEnables.TelMLost))
                .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new BridgeManagerService.BridgeManagerServiceClient(channel);

            var configureBeepResponse = aBridgeManagerClient.ConfigureBeep(new ConfigureBeepRequest
            {
                Name = "//summit/bridge/foo",
                BeepConfig = SummitBeepConfig.TelMLost,
            });

            mockDevice.Verify();

            var error = configureBeepResponse.Error;
            Assert.AreEqual(SummitApiErrorCode.NoError, error.RejectCode);

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.4 - Configure Beep
        [Test]
        public void ConfigureBeep_InvalidRequest_ReturnsError()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";

            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            var summitService = new SummitService(repository, mockManager.Object);

            // Construct invalid parameter CtmBeepEnables input
            byte ctmBeepEnablesByte = 0x09;
            Medtronic.TelemetryM.CtmProtocol.Commands.CtmBeepEnables ctmBeepEnablesState = (Medtronic.TelemetryM.CtmProtocol.Commands.CtmBeepEnables)ctmBeepEnablesByte;

            mockDevice.Setup(Bridge => Bridge.WriteTelemetrySoundEnables(ctmBeepEnablesState))
                .Returns(new APIReturnInfo(APIRejectCodes.InvalidParameter, 0, DateTime.Now, DateTime.Now, 0));

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new BridgeManagerService.BridgeManagerServiceClient(channel);

            // Make call on ConfigureBeep with invalid parameter request
            var configureBeepResponse = aBridgeManagerClient.ConfigureBeep(new ConfigureBeepRequest
            {
                Name = "//summit/bridge/foo",
                BeepConfig = (SummitBeepConfig)ctmBeepEnablesState,
            });

            mockDevice.Verify();

            var error = configureBeepResponse.Error;

            // Check that the response reflects an InvalidParameter error
            Assert.AreEqual(SummitApiErrorCode.InvalidParameter, error.RejectCode);

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.4 - Configure Beep
        [Test]
        public void ConfigureBeep_ErrorEncountered_ReturnsError()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";

            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            var summitService = new SummitService(repository, mockManager.Object);

            // Construct invalid parameter CtmBeepEnables input
            Medtronic.TelemetryM.CtmProtocol.Commands.CtmBeepEnables ctmBeepEnablesState = Medtronic.TelemetryM.CtmProtocol.Commands.CtmBeepEnables.NoDeviceDiscovered;

            mockDevice.Setup(Bridge => Bridge.WriteTelemetrySoundEnables(ctmBeepEnablesState))
                .Returns(new APIReturnInfo(APIRejectCodes.NoCtmConnected, 0, DateTime.Now, DateTime.Now, 0));

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new BridgeManagerService.BridgeManagerServiceClient(channel);

            // Make call on ConfigureBeep with invalid parameter request
            var configureBeepResponse = aBridgeManagerClient.ConfigureBeep(new ConfigureBeepRequest
            {
                Name = "//summit/bridge/foo",
                BeepConfig = (SummitBeepConfig)ctmBeepEnablesState,
            });

            mockDevice.Verify();

            var error = configureBeepResponse.Error;

            // Check that the response reflects an InvalidParameter error
            Assert.AreEqual(SummitApiErrorCode.NoCtmConnected, error.RejectCode);

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.9 - Configure Sense
        [Test]
        public void ConfigureSense_NullRequestParameter_ReturnsFailure()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            SenseConfigurationRequest senseConfigurationRequest = new SenseConfigurationRequest();
            senseConfigurationRequest.Name = "//summit/bridge/foo/device/foo";
            senseConfigurationRequest.AccelerometerConfig = null;
            var response = aDeviceManagerClient.SenseConfiguration(senseConfigurationRequest);
            Assert.IsTrue(response.Error.RejectCode != 0);
            Assert.IsTrue(response.Error.Message == "Object reference not set to an instance of an object.");
            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.9 - Configure Sense
        [Test]
        public void ConfigureSense_PowerBandEnablesCountIncorrect_ReturnsFailure()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connects to the service and initiates streaming, ensures that it's running (queue != null) before proceeding

            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            SenseConfigurationRequest senseConfigurationRequest = new SenseConfigurationRequest();
            senseConfigurationRequest.Name = "//summit/bridge/foo/device/foo";

            // TimeDomain configurations
            senseConfigurationRequest.TdChannelConfigs.Add(new List<SummitTimeDomainChannelConfig>
            {
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)4,
                    Minus = (OpenMind.TimeDomainMuxInputs)1,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)8,
                    Minus = (OpenMind.TimeDomainMuxInputs)2,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)4,
                    Minus = (OpenMind.TimeDomainMuxInputs)1,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)8,
                    Minus = (OpenMind.TimeDomainMuxInputs)2,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                }
            });

            // FFT configurations
            senseConfigurationRequest.FftConfig = new SummitFastFourierTransformStreamConfiguration
            {
                Size = (FastFourierTransformSizes)0,
                Interval = 100,
                WindowLoad = (FastFourierTransformWindowAutoLoads)0,
                EnableWindow = true,
                BandFormationConfig = (FastFourierTransformWeightMultiplies)0,
                BinsToStream = 0,
                BinsToStreamOffset = 0
            };

            // Power configurations
            senseConfigurationRequest.PowerChannelConfig = new SummitPowerStreamConfiguration();
            // Add just one value. Fails since it needs 8 total
            senseConfigurationRequest.PowerChannelConfig.PowerBandEnables.Add(true);
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });

            // Misc configurations
            senseConfigurationRequest.MiscStreamConfig = new MiscellaneousStreamConfiguration
            {
                Bridging = 0,
                StreamingRate = (OpenMind.StreamingFrameRate)6,
                LoopRecordTriggers = 0,
                LoopRecordingPostBufferTime = 53
            };

            // Acc configurations
            senseConfigurationRequest.AccelerometerConfig = new SummitAccelerometerStreamConfiguration
            {
                SampleRate = 0
            };

            var response = aDeviceManagerClient.SenseConfiguration(senseConfigurationRequest);
            Assert.IsTrue(response.Error.RejectCode != 0);
            Assert.IsTrue(response.Error.Message == "PowerBandEnables must be size of 8");

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.9 - Configure Sense
        [Test]
        public void ConfigureSense_PowerBandConfigurationCountIncorrect_ReturnsFailure()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            SenseConfigurationRequest senseConfigurationRequest = new SenseConfigurationRequest();
            senseConfigurationRequest.Name = "//summit/bridge/foo/device/foo";
            // TimeDomain configurations
            senseConfigurationRequest.TdChannelConfigs.Add(new List<SummitTimeDomainChannelConfig>
            {
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)4,
                    Minus = (OpenMind.TimeDomainMuxInputs)1,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)8,
                    Minus = (OpenMind.TimeDomainMuxInputs)2,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)4,
                    Minus = (OpenMind.TimeDomainMuxInputs)1,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)8,
                    Minus = (OpenMind.TimeDomainMuxInputs)2,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                }
            });

            // FFT configurations
            senseConfigurationRequest.FftConfig = new SummitFastFourierTransformStreamConfiguration
            {
                Size = (FastFourierTransformSizes)0,
                Interval = 100,
                WindowLoad = (FastFourierTransformWindowAutoLoads)0,
                EnableWindow = true,
                BandFormationConfig = (FastFourierTransformWeightMultiplies)0,
                BinsToStream = 0,
                BinsToStreamOffset = 0
            };

            // Power configurations
            senseConfigurationRequest.PowerChannelConfig = new SummitPowerStreamConfiguration();
            senseConfigurationRequest.PowerChannelConfig.PowerBandEnables.Add(new bool[] { true, true, true, true, true, true, true, true });
            // Add just one value. Fails since it needs 8 total
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration
            {
                BandStart = 0,
                BandStop = 0
            });

            // Misc configurations
            senseConfigurationRequest.MiscStreamConfig = new MiscellaneousStreamConfiguration
            {
                Bridging = 0,
                StreamingRate = (OpenMind.StreamingFrameRate)6,
                LoopRecordTriggers = 0,
                LoopRecordingPostBufferTime = 53
            };

            // Acc configurations
            senseConfigurationRequest.AccelerometerConfig = new SummitAccelerometerStreamConfiguration
            {
                SampleRate = 0
            };

            var response = aDeviceManagerClient.SenseConfiguration(senseConfigurationRequest);
            Assert.IsTrue(response.Error.RejectCode != 0);
            Assert.IsTrue(response.Error.Message == "PowerBandConfiguration must be size of 8");

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.9 - Configure Sense
        [Test]
        public void ConfigureSense_NullRequestParametersObject_ReturnsFailure()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            SenseConfigurationRequest senseConfigurationRequest = new SenseConfigurationRequest();
            senseConfigurationRequest.Name = "//summit/bridge/foo/device/foo";
            // TimeDomain configurations
            senseConfigurationRequest.TdChannelConfigs.Add(new List<SummitTimeDomainChannelConfig>
            {
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)4,
                    Minus = (OpenMind.TimeDomainMuxInputs)1,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)8,
                    Minus = (OpenMind.TimeDomainMuxInputs)2,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)4,
                    Minus = (OpenMind.TimeDomainMuxInputs)1,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)8,
                    Minus = (OpenMind.TimeDomainMuxInputs)2,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                }
            });

            // FFT Configuration
            senseConfigurationRequest.FftConfig = new SummitFastFourierTransformStreamConfiguration
            {
                Size = (FastFourierTransformSizes)0,
                Interval = 100,
                WindowLoad = (FastFourierTransformWindowAutoLoads)0,
                EnableWindow = true,
                BandFormationConfig = (FastFourierTransformWeightMultiplies)0,
                BinsToStream = 0,
                BinsToStreamOffset = 0
            };

            // Power configurations
            senseConfigurationRequest.PowerChannelConfig = new SummitPowerStreamConfiguration();
            senseConfigurationRequest.PowerChannelConfig.PowerBandEnables.Add(new bool[] { true, true, true, true, true, true, true, true });
            // Add just one value. Fails since it needs 8 total
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });

            // Misc configurations
            senseConfigurationRequest.MiscStreamConfig = new MiscellaneousStreamConfiguration
            {
                Bridging = 0,
                StreamingRate = (OpenMind.StreamingFrameRate)6,
                LoopRecordTriggers = 0,
                LoopRecordingPostBufferTime = 53
            };

            // Acc configuration - NULL

            var response = aDeviceManagerClient.SenseConfiguration(senseConfigurationRequest);
            Assert.IsTrue(response.Error.RejectCode != 0);
            Assert.IsTrue(response.Error.Message == "Object reference not set to an instance of an object.");

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.9 - Configure Sense
        [Test]
        public void ConfigureSense_WriteSensingFftSettingsReject_ReturnsFailure()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            SenseConfigurationRequest senseConfigurationRequest = new SenseConfigurationRequest();
            senseConfigurationRequest.Name = "//summit/bridge/foo/device/foo";
            // TimeDomain configurations
            senseConfigurationRequest.TdChannelConfigs.Add(new List<SummitTimeDomainChannelConfig>
            {
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)4,
                    Minus = (OpenMind.TimeDomainMuxInputs)1,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)8,
                    Minus = (OpenMind.TimeDomainMuxInputs)2,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)4,
                    Minus = (OpenMind.TimeDomainMuxInputs)1,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)8,
                    Minus = (OpenMind.TimeDomainMuxInputs)2,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                }
            });

            // FFT configurations
            senseConfigurationRequest.FftConfig = new SummitFastFourierTransformStreamConfiguration
            {
                Size = (FastFourierTransformSizes)0,
                Interval = 1,
                WindowLoad = (FastFourierTransformWindowAutoLoads)0,
                EnableWindow = true,
                BandFormationConfig = (FastFourierTransformWeightMultiplies)0,
                BinsToStream = 0,
                BinsToStreamOffset = 0
            };

            // Power configurations
            senseConfigurationRequest.PowerChannelConfig = new SummitPowerStreamConfiguration();
            senseConfigurationRequest.PowerChannelConfig.PowerBandEnables.Add(new bool[] { true, true, true, true, true, true, true, true });
            // Add just one value. Fails since it needs 8 total
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });

            // Misc configurations
            senseConfigurationRequest.MiscStreamConfig = new MiscellaneousStreamConfiguration
            {
                Bridging = 0,
                StreamingRate = (OpenMind.StreamingFrameRate)6,
                LoopRecordTriggers = 0,
                LoopRecordingPostBufferTime = 53
            };

            // Acc configurations
            senseConfigurationRequest.AccelerometerConfig = new SummitAccelerometerStreamConfiguration
            {
                SampleRate = 0
            };

            senseConfigurationRequest.SenseEnablesConfig = new SummitSenseEnablesConfiguration
            {
                EnableTimedomain = true,
                EnableFft = true
            };

            // We don't need to mock responses for WriteSensingState and WriteSensingState because the default error code for an unitialized response is 0 (success)

            // Mock device for WriteSensingFftSettings to return reject failure
            Medtronic.NeuroStim.Olympus.DataTypes.Sensing.FftConfiguration fftConfiguration = new Medtronic.NeuroStim.Olympus.DataTypes.Sensing.FftConfiguration()
            {
                Size = 0,
                Interval = 1,
                WindowLoad = 0,
                WindowEnabled = true,
                BandFormationConfig = 0,
                StreamSizeBins = 0,
                StreamOffsetBins = 0
            };
            mockDevice.Setup(Device => Device.WriteSensingFftSettings(fftConfiguration))
                .Returns(new APIReturnInfo(APIRejectCodes.Fft1024MaxUpdateRateExceeded, 0, DateTime.Now, DateTime.Now, 0));

            var response = aDeviceManagerClient.SenseConfiguration(senseConfigurationRequest);
            Assert.IsTrue(response.Error.RejectCode == (SummitApiErrorCode)APIRejectCodes.Fft1024MaxUpdateRateExceeded);

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.9 - Configure Sense
        [Test]
        public void ConfigureSense_DisabledTimeDomainStream_SetsSampleRateToDisabled()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            SenseConfigurationRequest senseConfigurationRequest = new SenseConfigurationRequest();
            senseConfigurationRequest.Name = "//summit/bridge/foo/device/foo";
            senseConfigurationRequest.TimedomainSamplingRate = 0x00;
            senseConfigurationRequest.TdChannelConfigs.Add(new List<SummitTimeDomainChannelConfig>
            {
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)4,
                    Minus = (OpenMind.TimeDomainMuxInputs)1,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = true,
                    Plus = (OpenMind.TimeDomainMuxInputs)8,
                    Minus = (OpenMind.TimeDomainMuxInputs)2,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)4,
                    Minus = (OpenMind.TimeDomainMuxInputs)1,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = true,
                    Plus = (OpenMind.TimeDomainMuxInputs)8,
                    Minus = (OpenMind.TimeDomainMuxInputs)2,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                }
            });
            senseConfigurationRequest.FftConfig = new SummitFastFourierTransformStreamConfiguration
            {
                Size = (FastFourierTransformSizes)0,
                Interval = 1,
                WindowLoad = (FastFourierTransformWindowAutoLoads)0,
                EnableWindow = true,
                BandFormationConfig = (FastFourierTransformWeightMultiplies)0,
                BinsToStream = 0,
                BinsToStreamOffset = 0
            };

            // Power configurations
            senseConfigurationRequest.PowerChannelConfig = new SummitPowerStreamConfiguration();
            senseConfigurationRequest.PowerChannelConfig.PowerBandEnables.Add(new bool[] { true, true, true, true, true, true, true, true });
            // Add just one value. Fails since it needs 8 total
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });

            // Misc configurations
            senseConfigurationRequest.MiscStreamConfig = new MiscellaneousStreamConfiguration
            {
                Bridging = 0,
                StreamingRate = (OpenMind.StreamingFrameRate)6,
                LoopRecordTriggers = 0,
                LoopRecordingPostBufferTime = 53
            };

            // Acc configurations
            senseConfigurationRequest.AccelerometerConfig = new SummitAccelerometerStreamConfiguration
            {
                SampleRate = 0
            };

            senseConfigurationRequest.SenseEnablesConfig = new SummitSenseEnablesConfiguration
            {
                EnableTimedomain = true,
                EnableAdaptiveStim = false,
                EnableFft = false,
                EnableLd0 = false,
                EnableLd1 = false,
                EnableLoopRecording = false,
                EnablePower = false,
            };

            List<TimeDomainChannel> arguments = null;

            mockDevice.Setup(Device => Device.WriteSensingTimeDomainChannels(It.IsAny<List<TimeDomainChannel>>()))
                .Callback<List<TimeDomainChannel>>((channels) => arguments = channels)
                .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));

            var response = aDeviceManagerClient.SenseConfiguration(senseConfigurationRequest);

            mockDevice.Verify(Device => Device.WriteSensingTimeDomainChannels(It.IsAny<List<TimeDomainChannel>>()), Times.Once());

            Assert.That(arguments != null);
            Assert.That(arguments.Count == 4);
            Assert.That(arguments[0].SampleRate == TdSampleRates.Sample0250Hz);
            Assert.That(arguments[1].SampleRate == TdSampleRates.Disabled);
            Assert.That(arguments[2].SampleRate == TdSampleRates.Sample0250Hz);
            Assert.That(arguments[3].SampleRate == TdSampleRates.Disabled);

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.9 - Configure Sense
        [Test]
        public void ConfigureSense_ValidConfiguration_Success()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            SenseConfigurationRequest senseConfigurationRequest = new SenseConfigurationRequest();
            senseConfigurationRequest.Name = "//summit/bridge/foo/device/foo";
            // TimeDomain configurations
            senseConfigurationRequest.TdChannelConfigs.Add(new List<SummitTimeDomainChannelConfig>
            {
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)4,
                    Minus = (OpenMind.TimeDomainMuxInputs)1,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)8,
                    Minus = (OpenMind.TimeDomainMuxInputs)2,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)4,
                    Minus = (OpenMind.TimeDomainMuxInputs)1,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                },
                new SummitTimeDomainChannelConfig
                {
                    Disabled = false,
                    Plus = (OpenMind.TimeDomainMuxInputs)8,
                    Minus = (OpenMind.TimeDomainMuxInputs)2,
                    EvokedMode = 0,
                    LowPassFilterStage1 = (OpenMind.TimeDomainLowPassFilterStage1)9,
                    LowPassFilterStage2 = (OpenMind.TimeDomainLowPassFilterStage2)9,
                    HighPassFilters = 0
                }
            });

            // FFT Configuration
            senseConfigurationRequest.FftConfig = new SummitFastFourierTransformStreamConfiguration
            {
                Size = (FastFourierTransformSizes)0,
                Interval = 100,
                WindowLoad = (FastFourierTransformWindowAutoLoads)0,
                EnableWindow = true,
                BandFormationConfig = (FastFourierTransformWeightMultiplies)0,
                BinsToStream = 0,
                BinsToStreamOffset = 0
            };

            // Power configurations
            senseConfigurationRequest.PowerChannelConfig = new SummitPowerStreamConfiguration();
            senseConfigurationRequest.PowerChannelConfig.PowerBandEnables.Add(new bool[] { true, false, false, false, false, false, false, false });
            // Add just one value. Fails since it needs 8 total
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });
            senseConfigurationRequest.PowerChannelConfig.PowerBandConfiguration.Add(new PowerBandConfiguration { BandStart = 0, BandStop = 0 });

            // Misc configurations
            senseConfigurationRequest.MiscStreamConfig = new MiscellaneousStreamConfiguration
            {
                Bridging = 0,
                StreamingRate = (OpenMind.StreamingFrameRate)6,
                LoopRecordTriggers = 0,
                LoopRecordingPostBufferTime = 53
            };

            // Acc configurations
            senseConfigurationRequest.AccelerometerConfig = new SummitAccelerometerStreamConfiguration
            {
                SampleRate = 0
            };
            senseConfigurationRequest.SenseEnablesConfig = new SummitSenseEnablesConfiguration
            {
                EnableTimedomain = true,
                EnableAdaptiveStim = false,
                EnableFft = false,
                EnableLd0 = false,
                EnableLd1 = false,
                EnableLoopRecording = false,
                EnablePower = false,
            };

            // Mock device for WriteSensingFftSettings
            Medtronic.NeuroStim.Olympus.DataTypes.Sensing.FftConfiguration fftConfiguration = new Medtronic.NeuroStim.Olympus.DataTypes.Sensing.FftConfiguration()
            {
                Size = 0,
                Interval = 100,
                WindowLoad = 0,
                WindowEnabled = true,
                BandFormationConfig = 0,
                StreamSizeBins = 0,
                StreamOffsetBins = 0
            };
            mockDevice.Setup(Device => Device.WriteSensingFftSettings(fftConfiguration))
            .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));

            // TD 
            List<Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TimeDomainChannel> timeDomainChannels = new List<Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TimeDomainChannel>();
            timeDomainChannels.Add(new Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TimeDomainChannel()
            {
                SampleRate = 0,
                MinusInput = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdMuxInputs)1,
                PlusInput = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdMuxInputs) 4,
                Lpf1 = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdLpfStage1)9,
                Lpf2 = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdLpfStage2)9,
                EvokedMode = 0,
                Hpf = 0,
            });
            timeDomainChannels.Add(new Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TimeDomainChannel()
            {
                SampleRate = 0,
                MinusInput = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdMuxInputs)2,
                PlusInput = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdMuxInputs)8,
                Lpf1 = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdLpfStage1)9,
                Lpf2 = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdLpfStage2)9,
                EvokedMode = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdEvokedResponseEnable)0,
                Hpf = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdHpfs)0,
            });
            timeDomainChannels.Add(new Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TimeDomainChannel()
            {
                SampleRate = 0,
                MinusInput = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdMuxInputs)1,
                PlusInput = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdMuxInputs)4,
                Lpf1 = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdLpfStage1)9,
                Lpf2 = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdLpfStage2)9,
                EvokedMode = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdEvokedResponseEnable)0,
                Hpf = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdHpfs)0,
            });
            timeDomainChannels.Add(new Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TimeDomainChannel()
            {
                SampleRate = 0, 
                MinusInput = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdMuxInputs)2,
                PlusInput = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdMuxInputs)8,
                Lpf1 = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdLpfStage1)9,
                Lpf2 = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.TdLpfStage2)9,
                EvokedMode = 0,
                Hpf = 0,
            });
            mockDevice.Setup(Device => Device.WriteSensingTimeDomainChannels(timeDomainChannels))
                .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));

            // Sample rate
            Medtronic.NeuroStim.Olympus.DataTypes.Sensing.AccelSampleRate accelSampleRate = 0;

            mockDevice.Setup(Device => Device.WriteSensingAccelSettings(accelSampleRate))
            .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));

            // Misc
            Medtronic.NeuroStim.Olympus.DataTypes.Sensing.MiscellaneousSensing miscellaneousSensing = new Medtronic.NeuroStim.Olympus.DataTypes.Sensing.MiscellaneousSensing()
            {
                Bridging = 0,
                StreamingRate = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.StreamingFrameRate)6,
                LrTriggers = 0,
                LrPostBufferTime = 53
            };

            mockDevice.Setup(Device => Device.WriteSensingMiscSettings(miscellaneousSensing))
            .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));

            // Power
            Medtronic.NeuroStim.Olympus.DataTypes.Sensing.BandEnables bandEnables = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.BandEnables)1;
            List<Medtronic.NeuroStim.Olympus.DataTypes.Sensing.PowerChannel> powerChannels = new List<Medtronic.NeuroStim.Olympus.DataTypes.Sensing.PowerChannel>();
            powerChannels.Add(new Medtronic.NeuroStim.Olympus.DataTypes.Sensing.PowerChannel()
            {
                Band0Start = 0,
                Band0Stop = 0, 
                Band1Start = 0,
                Band1Stop = 0,  
            });
            powerChannels.Add(new Medtronic.NeuroStim.Olympus.DataTypes.Sensing.PowerChannel()
            {
                Band0Start = 0,
                Band0Stop = 0,
                Band1Start = 0,
                Band1Stop = 0,
            });
            powerChannels.Add(new Medtronic.NeuroStim.Olympus.DataTypes.Sensing.PowerChannel()
            {
                Band0Start = 0,
                Band0Stop = 0,
                Band1Start = 0,
                Band1Stop = 0,
            });
            powerChannels.Add(new Medtronic.NeuroStim.Olympus.DataTypes.Sensing.PowerChannel()
            {
                Band0Start = 0,
                Band0Stop = 0,
                Band1Start = 0,
                Band1Stop = 0,
            });          

            mockDevice.Setup(Device => Device.WriteSensingPowerChannels(bandEnables, powerChannels ))
                .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));

            // Sense Enables
            Medtronic.NeuroStim.Olympus.DataTypes.Sensing.SenseStates senseStates = (Medtronic.NeuroStim.Olympus.DataTypes.Sensing.SenseStates)1;
            Medtronic.NeuroStim.Olympus.DataTypes.Sensing.SenseTimeDomainChannel senseTimeDomainChannel = 0;
            mockDevice.Setup(Device => Device.WriteSensingState(senseStates,senseTimeDomainChannel))
                .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));

            var response = aDeviceManagerClient.SenseConfiguration(senseConfigurationRequest);
            Console.WriteLine(response);
            mockDevice.Verify(Device => Device.WriteSensingTimeDomainChannels(timeDomainChannels), Times.Once());
            mockDevice.Verify(Device => Device.WriteSensingFftSettings(fftConfiguration), Times.Once());
            mockDevice.Verify(Device => Device.WriteSensingAccelSettings(accelSampleRate), Times.Once());
            mockDevice.Verify(Device => Device.WriteSensingMiscSettings(miscellaneousSensing), Times.Once());
            mockDevice.Verify(Device => Device.WriteSensingPowerChannels(bandEnables, powerChannels), Times.Once());
            mockDevice.Verify(Device => Device.WriteSensingState(senseStates, senseTimeDomainChannel), Times.Once());

            Assert.IsTrue(response.Error.RejectCode == (SummitApiErrorCode)APIRejectCodes.NoError);

            // Close out the server
            aTestServer.StopServer();
        } 

        // Tests Requirement 5.1.1 - Initialization of Services
        // Tests Requirement 5.2.1 - Info Service Functions
        [Test]
        public void Info_Service_InspectRepository()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();

            string[] testRepoUris = { "//summit/bridge/foo", "//summit/bridge/boo" } ;
            foreach (var testRepoUri in testRepoUris)
            {
                repository.CacheConnection(testRepoUri, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
                repository.GetConnectionByName(testRepoUri).SetDeviceSerial("bar");
            }

            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.InfoService.InfoServiceClient aInfoServiceClient = new OpenMind.InfoService.InfoServiceClient(channel);

            var repository_results = aInfoServiceClient.InspectRepository(new InspectRepositoryRequest() { });
            foreach (var aRepoUri in repository_results.RepoUri)
            {
                Assert.IsTrue(testRepoUris.Any(s => s.Equals(aRepoUri)));
                Assert.That(aRepoUri != null);
            }
            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.1 - Info Service Functions
        [Test]
        public void Info_Service_VersionNumber()
        {
            var mockManager = new Mock<ISummitManager>();
            var repository = Repository.GetRepositoryInstance();
            
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.InfoService.InfoServiceClient aInfoServiceClient = new OpenMind.InfoService.InfoServiceClient(channel);

            var repository_results = aInfoServiceClient.VersionNumber(new VersionNumberRequest() { });

            string actualVersion = System.Reflection.Assembly.GetAssembly(typeof(OmniServer)).GetName().Version.ToString();
            Assert.That(repository_results.VersionNumber == actualVersion);

            // Close out the server
            aTestServer.StopServer();

        }

        // Tests Requirement 5.2.1 - Info Service Functions
        [Test]
        public void Info_Service_SuppotedDevices()
        {
            var mockManager = new Mock<ISummitManager>();
            var repository = Repository.GetRepositoryInstance();

            string[] testSupportedDevices = { "Medtronic Summit RC+S" };

            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.InfoService.InfoServiceClient aInfoServiceClient = new OpenMind.InfoService.InfoServiceClient(channel);

            var supportedDevices_result = aInfoServiceClient.SupportedDevices(new SupportedDevicesRequest() { });

            Assert.That(supportedDevices_result.SupportedDevices != null);
            Assert.AreEqual(supportedDevices_result.SupportedDevices, testSupportedDevices);

            // Close out the server
            aTestServer.StopServer();

        }

        // Tests Requirement 5.2.9 - Stream Enable
        [Test]
        public void StreamEnable_ValidRequestParameter_ReturnsSuccess()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            mockDevice.Setup(m => m.WriteSensingEnableStreams(true, true, true, true, true, true, true, true))

            .Returns(new Medtronic.SummitAPI.Classes.APIReturnInfo(
                Medtronic.SummitAPI.Classes.APIRejectCodes.NoError,
                Medtronic.NeuroStim.Olympus.Commands.CommandCodes.Unused00,
                DateTime.Now,
                DateTime.Now,
                0
            ))
            .Verifiable();

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            // Create request 
            StreamConfigureRequest streamConfigureRequest = new StreamConfigureRequest();

            // Add name parameter 
            streamConfigureRequest.Name = "//summit/bridge/foo/device/foo";

            // Add SummitStreamEnablesConfiguration parameter 
            streamConfigureRequest.Parameters = new SummitStreamEnablesConfiguration
            {
                EnableTimedomain = true,
                EnableFft = true,
                EnablePower = true,
                EnableAccelerometry = true,
                EnableDetector = true,
                EnableAdaptiveState = true,
                EnableLoopRecordMarkerEcho = true,
                EnableTime = true
            };

            // Get client respose
            var response = aDeviceManagerClient.StreamEnable(streamConfigureRequest);

            // Close out the server
            aTestServer.StopServer();

            // Verifies that setup mock functions were called
            mockDevice.VerifyAll();


            Assert.IsTrue(response.StreamConfigureStatus == OpenMind.StreamConfigureStatus.Success);
        }

        // Tests Requirement 5.2.9 - Stream Enable
        [Test]
        public void StreamEnable_ErrorEncountered_ReturnsFailure()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            mockDevice.Setup(m => m.WriteSensingEnableStreams(false, true, false, false, false, false, false, false))
            .Returns(new Medtronic.SummitAPI.Classes.APIReturnInfo(
                Medtronic.SummitAPI.Classes.APIRejectCodes.NoCtmConnected,
                Medtronic.NeuroStim.Olympus.Commands.CommandCodes.Unused00,
                DateTime.Now,
                DateTime.Now,
                0
            ))
            .Verifiable();

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            // Create request 
            StreamConfigureRequest streamConfigureRequest = new StreamConfigureRequest();

            // Add name parameter 
            streamConfigureRequest.Name = "//summit/bridge/foo/device/foo";

            // Add SummitStreamEnablesConfiguration parameter 
            streamConfigureRequest.Parameters = new SummitStreamEnablesConfiguration
            {
                EnableTimedomain = false,
                EnableFft = true,
                EnablePower = false,
                EnableAccelerometry = false,
                EnableDetector = false,
                EnableAdaptiveState = false,
                EnableLoopRecordMarkerEcho = false,
                EnableTime = false
            };

            // Get client response
            var response = aDeviceManagerClient.StreamEnable(streamConfigureRequest);

            // Close out the server
            aTestServer.StopServer();

            // Verifies that setup mock functions were called
            mockDevice.VerifyAll();


            Assert.IsTrue(response.StreamConfigureStatus == OpenMind.StreamConfigureStatus.Failure);
        }

        // Tests Requirement 5.2.9 - Stream Enable
        [Test]
        public void StreamEnable_InvalidRequestParameter_ReturnsUnknown()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            // Create request 
            StreamConfigureRequest streamConfigureRequest = new StreamConfigureRequest();

            // Add name parameter 
            streamConfigureRequest.Name = "//summit/bridge/bar/device/foo";

            // Add SummitStreamEnablesConfiguration parameter 
            streamConfigureRequest.Parameters = new SummitStreamEnablesConfiguration
            {
                EnableTimedomain = false,
                EnableFft = true,
                EnablePower = true,
                EnableAccelerometry = true,
                EnableDetector = true,
                EnableAdaptiveState = true,
                EnableLoopRecordMarkerEcho = true,
                EnableTime = true
            };

            // Get client response
            var response = aDeviceManagerClient.StreamEnable(streamConfigureRequest);

            // Close out the server
            aTestServer.StopServer();

            Assert.IsTrue(response.StreamConfigureStatus == OpenMind.StreamConfigureStatus.Unknown);
        }

        // Tests Requirement 5.2.9 - Stream Disable
        [Test]
        public void StreamDisable_ValidRequestParameter_ReturnsSuccess()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            mockDevice.Setup(m => m.WriteSensingDisableStreams(true, true, true, true, true, true, true, true))
            .Returns(new Medtronic.SummitAPI.Classes.APIReturnInfo(
                Medtronic.SummitAPI.Classes.APIRejectCodes.NoError,
                Medtronic.NeuroStim.Olympus.Commands.CommandCodes.Unused00,
                DateTime.Now,
                DateTime.Now,
                0
            ))
            .Verifiable();

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            // Create request 
            StreamConfigureRequest streamConfigureRequest = new StreamConfigureRequest();

            // Add name parameter 
            streamConfigureRequest.Name = "//summit/bridge/foo/device/foo";

            // Add SummitStreamEnablesConfiguration parameter 
            streamConfigureRequest.Parameters = new SummitStreamEnablesConfiguration
            {
                EnableTimedomain = true,
                EnableFft = true,
                EnablePower = true,
                EnableAccelerometry = true,
                EnableDetector = true,
                EnableAdaptiveState = true,
                EnableLoopRecordMarkerEcho = true,
                EnableTime = true
            };

            // Get client respose
            var response = aDeviceManagerClient.StreamDisable(streamConfigureRequest);

            // Close out the server
            aTestServer.StopServer();

            // Verifies that setup mock functions were called
            mockDevice.VerifyAll();


            Assert.IsTrue(response.StreamConfigureStatus == OpenMind.StreamConfigureStatus.Success);
        }

        // Tests Requirement 5.2.9 - Stream Disable
        [Test]
        public void StreamDisable_ErrorEncountered_ReturnsFailure()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            mockDevice.Setup(m => m.WriteSensingDisableStreams(true, true, true, true, true, true, true, true))
            .Returns(new Medtronic.SummitAPI.Classes.APIReturnInfo(
                Medtronic.SummitAPI.Classes.APIRejectCodes.NoCtmConnected,
                Medtronic.NeuroStim.Olympus.Commands.CommandCodes.Unused00,
                DateTime.Now,
                DateTime.Now,
                0
            ))
            .Verifiable();

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            // Create request 
            StreamConfigureRequest streamConfigureRequest = new StreamConfigureRequest();

            // Add name parameter 
            streamConfigureRequest.Name = "//summit/bridge/foo/device/foo";

            // Add SummitStreamEnablesConfiguration parameter 
            streamConfigureRequest.Parameters = new SummitStreamEnablesConfiguration
            {
                EnableTimedomain = true,
                EnableFft = true,
                EnablePower = true,
                EnableAccelerometry = true,
                EnableDetector = true,
                EnableAdaptiveState = true,
                EnableLoopRecordMarkerEcho = true,
                EnableTime = true
            };

            // Get client respose
            var response = aDeviceManagerClient.StreamDisable(streamConfigureRequest);

            // Close out the server
            aTestServer.StopServer();

            // Verifies that setup mock functions were called
            mockDevice.VerifyAll();


            Assert.IsTrue(response.StreamConfigureStatus == OpenMind.StreamConfigureStatus.Failure);
        }

        // Tests Requirement 5.2.9 - Stream Disable
        [Test]
        public void StreamDisable_InvalidRequestParameter_ReturnsUnknown()
        {
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            var name = "//summit/bridge/foo";
            repository.CacheConnection(name, mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());

            var summitService = new SummitService(repository, mockManager.Object);

            // Create the gRPC server to host the test
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new DeviceManagerService.DeviceManagerServiceClient(channel);

            // Create request 
            StreamConfigureRequest streamConfigureRequest = new StreamConfigureRequest();

            // Add name parameter 
            streamConfigureRequest.Name = "//summit/bridge/bar/device/foo";

            // Add SummitStreamEnablesConfiguration parameter 
            streamConfigureRequest.Parameters = new SummitStreamEnablesConfiguration
            {
                EnableTimedomain = true,
                EnableFft = true,
                EnablePower = true,
                EnableAccelerometry = true,
                EnableDetector = true,
                EnableAdaptiveState = true,
                EnableLoopRecordMarkerEcho = true,
                EnableTime = true
            };

            // Get client respose
            var response = aDeviceManagerClient.StreamDisable(streamConfigureRequest);

            // Close out the server
            aTestServer.StopServer();

            Assert.IsTrue(response.StreamConfigureStatus == OpenMind.StreamConfigureStatus.Unknown);
        }
    }
}
    

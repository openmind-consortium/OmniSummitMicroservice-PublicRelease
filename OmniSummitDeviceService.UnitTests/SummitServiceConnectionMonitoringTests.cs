using Grpc.Core;
using Medtronic.NeuroStim.Olympus.DataTypes.PowerManagement;
using Medtronic.SummitAPI.Events;
using Medtronic.SummitAPI.Classes;
using Medtronic.TelemetryM.CtmProtocol.Commands;
using Medtronic.TelemetryM;
using Moq;
using NUnit.Framework;
using OpenMind;
using OpenMindServer.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Threading;

namespace OpenMindServer.UnitTests
{
    [TestFixture]
    public partial class SummitServiceTest
    {
        #region Bridge Connection Status Stream Tests
        // Tests Requirement 5.2.5 - Connection Monitoring (management of stream)
        [Test]
        public async Task ConnectionStatusStream_NoCachedConnection_ReturnsEmpty()
        {
            // First, create a summit service
            var mockManager = new Mock<ISummitManager>();
            var repository = Repository.GetRepositoryInstance();
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connects to the service.
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.BridgeManagerService.BridgeManagerServiceClient aDeviceManagerClient = new OpenMind.BridgeManagerService.BridgeManagerServiceClient(channel);

            // Attempt to connect to the stream when there is no device connected
            var stream = aDeviceManagerClient.ConnectionStatusStream(new SetStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo" });

            // Expected behavior is that the returned stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(await stream.ResponseStream.MoveNext());
            Assert.IsTrue(stream.GetStatus().StatusCode == StatusCode.OK);

            // Close out the server
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.5 - Connection Monitoring (management of stream)
        [Test]
        public async Task ConnectionStatusStream_AlreadyConnected_ReturnsEmpty()
        {
            // First, create a summit service with a cached connected device
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            repository.CacheConnection("//summit/bridge/foo", mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            repository.GetConnectionByName("//summit/bridge/foo").SetDeviceSerial("bar");
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create "A" client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channelA = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.BridgeManagerService.BridgeManagerServiceClient aDeviceManagerClientA = new OpenMind.BridgeManagerService.BridgeManagerServiceClient(channelA);
            var streamA = aDeviceManagerClientA.ConnectionStatusStream(new SetStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.ConnectionStatusQueue == null)
            {
                await Task.Delay(100);
                count++;

                // Check for failure condition
                if (count >= 10)
                {
                    // Failed to connect, leave test rig in correct form by closing out the server
                    aTestServer.StopServer();

                    // Indicate test failed due to a timeout
                    Assert.Fail("Initial Connect Timeout");

                    // Do not proceed with remainder of test
                    return;
                }
            };

            // "B" Client attempts to initiate stream
            Channel channelB = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.BridgeManagerService.BridgeManagerServiceClient aDeviceManagerClientB = new OpenMind.BridgeManagerService.BridgeManagerServiceClient(channelB);
            var streamB = aDeviceManagerClientB.ConnectionStatusStream(new SetStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });

            // Save test values for later asserts
            bool streamMoveNext = await streamB.ResponseStream.MoveNext();
            bool streamStatusCode = streamB.GetStatus().StatusCode == StatusCode.OK;

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that the returned stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext, "Move Next returned true, expected false");
            Assert.IsTrue(streamStatusCode, "Get status was not 'OK'");
        }

        // Tests Requirement 5.2.5 - Connection Monitoring (management of stream)
        [Test]
        public async Task ConnectionStatusStream_EnableStreamRequest_ReturnsStream()
        {
            // First, create a summit service with a cached connected device
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            repository.CacheConnection("//summit/bridge/foo", mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            repository.GetConnectionByName("//summit/bridge/foo").SetDeviceSerial("bar");
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connects to the service and initiates streaming, ensures that it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new OpenMind.BridgeManagerService.BridgeManagerServiceClient(channel);
            var stream = aBridgeManagerClient.ConnectionStatusStream(new SetStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.ConnectionStatusQueue == null)
            {
                await Task.Delay(100);
                count++;

                // Check for failure condition
                if (count >= 10)
                {
                    // Failed to connect, leave test rig in correct form by closing out the server
                    aTestServer.StopServer();

                    // Indicate test failed due to a timeout
                    Assert.Fail("Initial Connect Timeout");

                    // Do not proceed with remainder of test
                    return;
                }
            };

            // Use the mock framework to raise a connection status event 
            theCachedConnection.ConnectionStatusQueue.Add(new ConnectionUpdate() { ConnectionStatus = "Test String", Name = "//summit/bridge/foo" });

            // Save test values for later asserts
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            ConnectionUpdate theReceivedUpdate = stream.ResponseStream.Current;

            // Close out the server
            aTestServer.StopServer();

            // Read the event out of the client stream and check values
            Assert.IsTrue(streamMoveNext);
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "Test String");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo");
        }

        // Tests Requirement 5.2.5 - Connection Monitoring (management of stream)
        [Test]
        public async Task ConnectionStatusStream_ConnectedDisableStream_EndsStream()
        {
            // First, create a summit service with a cached connected device
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            repository.CacheConnection("//summit/bridge/foo", mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            repository.GetConnectionByName("//summit/bridge/foo").SetDeviceSerial("bar");
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connects to the service and initiates streaming, ensures that it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new OpenMind.BridgeManagerService.BridgeManagerServiceClient(channel);
            var stream = aBridgeManagerClient.ConnectionStatusStream(new SetStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.ConnectionStatusQueue == null)
            {
                await Task.Delay(100);
                count++;

                // Check for failure condition
                if (count >= 10)
                {
                    // Failed to connect, leave test rig in correct form by closing out the server
                    aTestServer.StopServer();

                    // Indicate test failed due to a timeout
                    Assert.Fail("Initial Connect Timeout");

                    // Do not proceed with remainder of test
                    return;
                }
            };

            // Stop the stream
            aBridgeManagerClient.ConnectionStatusStream(new SetStreamEnable() { EnableStream = false, Name = "//summit/bridge/foo/device/bar" });

            // Save test values for later asserts
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            bool streamStatusCode = stream.GetStatus().StatusCode == StatusCode.OK;

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that the closed stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext);
            Assert.IsTrue(streamStatusCode);
        }

        #endregion Bridge Connection Status Stream Tests

        #region Reconnection Logic Tests
        // Tests Requirement 5.2.5 - Connection Monitoring (updates sent to client)
        [Test]
        public async Task ConnectionMonitoring_DisconnectionEventReceivedSummitNotDisposed_Notify()
        {
            // This test evaluates the OMNI connection stream responding with the correct behavior when the CTM is disconnected and then successfully reconnected by the Summit API without OMNI intervention
            // Expected behavior of SummitSystem Object:
            // - Summit API indicates disconnection occuring by calling event handler, returns "NoCtmConnected" and "NoInsConnected" return codes while reconnecting.
            // - Summit API indicates success by returning API commands with "NoError"
            // - Summit API does not mark device as disposed
            // Expected behavior or OMNI:
            // - Reports CTM as disconnected
            // - polls Summit API until "NoError" is returned
            // - Reports CTM as reconnected

            // First, create a summit service with a cached connected device
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            repository.CacheConnection("//summit/bridge/foo", mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            repository.GetConnectionByName("//summit/bridge/foo").SetDeviceSerial("bar");
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connects to the service and initiates streaming, ensures that it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new OpenMind.BridgeManagerService.BridgeManagerServiceClient(channel);
            var stream = aBridgeManagerClient.ConnectionStatusStream(new SetStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.ConnectionStatusQueue == null)
            {
                await Task.Delay(100);
                Assert.Less(count++, 10, "Initial Connect Timeout");
            };

            // Summit should be indicate it is in a disposed state for this test
            BatteryStatusResult theBatteryStatusResult;
            mockDevice.SetupSequence(Device => Device.ReadBatteryLevel(out theBatteryStatusResult))
                .Returns(new APIReturnInfo(APIRejectCodes.NoCtmConnected, 0, DateTime.Now, DateTime.Now, 0))
                .Returns(new APIReturnInfo(APIRejectCodes.NoInsConnected, 0, DateTime.Now, DateTime.Now, 0))
                .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));
            mockDevice.Setup(Device => Device.IsDisposed).Returns(false);

            // Use the mock framework to raise a connection status event
            Thread threadedEvent = new Thread(() =>
            {
                mockDevice.Raise(m => m.UnexpectedCtmDisconnectHandler += null, new EventArgs());
            });
            threadedEvent.Start();

            // Read the event out of the client stream and check for CTM disconnected notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read first message");
            ConnectionUpdate theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "CTM Disconnected!", "Incorrect status in first message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in first message");

            // Read the event out of the client stream and check for CTM disposed notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read second message");
            theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "SummitSystem Reconnected!", "Incorrect status in second message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in second message");

            // Close out the server
            threadedEvent.Join();
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.5 - Connection Monitoring (updates sent to client)
        [Test]
        public async Task ConnectionMonitoring_DisconnectionEventReceivedSummitDisposed_Notify()
        {
            // This test evaluates the OMNI connection stream responding with the correct behavior when the CTM is disconnected and cannot be reconnected to by the Summit API without OMNI intervention
            // Expected behavior of SummitSystem Object:
            // - Summit API indicates disconnection occuring by calling event handler, returns "NoCtmConnected" and "NoInsConnected" return codes while reconnecting.
            // - Summit API disposes the SummitSystem object when it fails to reconnect
            // Expected behavior or OMNI:
            // - Reports CTM as disconnected
            // - polls Summit API until "NoError" is returned
            // - Reports CTM is disposed

            // First, create a summit service with a cached connected device
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            repository.CacheConnection("//summit/bridge/foo", mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            repository.GetConnectionByName("//summit/bridge/foo").SetDeviceSerial("bar");
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connects to the service and initiates streaming, ensures that it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new OpenMind.BridgeManagerService.BridgeManagerServiceClient(channel);
            var stream = aBridgeManagerClient.ConnectionStatusStream(new SetStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.ConnectionStatusQueue == null)
            {
                await Task.Delay(100);
                Assert.Less(count++, 10, "Initial Connect Timeout");
            };

            // After first battery poll Summit should indicate it is in a disposed state for this test
            BatteryStatusResult theBatteryStatusResult;
            mockDevice.SetupSequence(Device => Device.ReadBatteryLevel(out theBatteryStatusResult))
                .Returns(new APIReturnInfo(APIRejectCodes.NoCtmConnected, 0, DateTime.Now, DateTime.Now, 0))
                .Returns(new APIReturnInfo(APIRejectCodes.IsDisposed, 0, DateTime.Now, DateTime.Now, 0));
            mockDevice.SetupSequence(Device => Device.IsDisposed)
                .Returns(false)
                .Returns(true);

            // Use the mock framework to raise a connection status event
            Thread threadedEvent = new Thread(() =>
            {
                mockDevice.Raise(m => m.UnexpectedCtmDisconnectHandler += null, new EventArgs());
            });
            threadedEvent.Start();

            // Read the event out of the client stream and check for CTM disconnected notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read first message");
            ConnectionUpdate theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "CTM Disconnected!", "Incorrect status in first message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in first message");

            // Read the event out of the client stream and check for CTM disposed notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read second message");
            theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "CTM Disposed!", "Incorrect status in second message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in second message");

            // Read the event out of the client stream and check for CTM connection failed notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read third message");
            theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "SummitSystem Reconnection Failed!", "Incorrect status in third message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in third message");

            // Wait until the threading disposal is complete and then close out the server
            threadedEvent.Join();
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.5 - Connection Monitoring (updates sent to client)
        [Test]
        public async Task ConnectionMonitoring_DisconnectionEventReceived_ReconnectionCTMAttemptsExceeded()
        {
            // This test evaluates the OMNI connection stream responding with the correct behavior when the CTM is disconnected and unable to be reconnected to by OMNI due to an unreconnectable CTM
            // Expected behavior of SummitSystem Object:
            // - Summit API indicates disconnection occuring by calling event handler, returns "NoCtmConnected" return codes while reconnecting.
            // - Summit API indicates failure in API-reconnect by returning API commands with "IsDisposed"
            // Expected behavior of OMNI:
            // - Reports CTM as disconnected
            // - polls Summit API until "NoError" is returned
            // - Reports CTM is disposed
            // - attempts to reconnect to CTM 4 times (defined by retry parameter)
            // - each attempt fails with an appropriate message on connection monitoring stream indicating continued failure to connect to CTM
            // - after 4 tries, stream indicates end of attempts and that reconnection has failed

            // First, create a summit service with a cached connected device
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            ConnectBridgeRequest testBridgeRequest = new ConnectBridgeRequest() { Retries = 4, PhysicalLayer = SummitPhysicalLayer.Any, TelemetryRatio = 8, TelemetryMode = "4", BeepEnables = SummitBeepConfig.DeviceDiscovered };
            repository.CacheConnection("//summit/bridge/foo", mockDevice.Object, mockManager.Object, testBridgeRequest, new InstrumentInfo()); ;
            repository.GetConnectionByName("//summit/bridge/foo").SetDeviceSerial("bar");
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connects to the service and initiates streaming, ensures that it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new OpenMind.BridgeManagerService.BridgeManagerServiceClient(channel);
            var stream = aBridgeManagerClient.ConnectionStatusStream(new SetStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.ConnectionStatusQueue == null)
            {
                await Task.Delay(100);
                Assert.Less(count++, 10, "Initial Connect Timeout");
            };

            // After first battery poll Summit should indicate it is in a disposed state for this test
            BatteryStatusResult theBatteryStatusResult;
            mockDevice.SetupSequence(Device => Device.ReadBatteryLevel(out theBatteryStatusResult))
                .Returns(new APIReturnInfo(APIRejectCodes.NoCtmConnected, 0, DateTime.Now, DateTime.Now, 0))    // Initial Battery poll while API is still attempting to reconnect
                .Returns(new APIReturnInfo(APIRejectCodes.IsDisposed, 0, DateTime.Now, DateTime.Now, 0));       // Battery poll when API is done reconnect
            mockDevice.SetupSequence(Device => Device.IsDisposed)
                .Returns(false)                                                                                 // API is still attempting to reconnect
                .Returns(true);                                                                                 // API is done reconnect
            ISummitSystem aSummitSystemResult;
            mockManager.Setup(Manager => Manager.CreateSummit(out aSummitSystemResult, new InstrumentInfo(), (InstrumentPhysicalLayers)testBridgeRequest.PhysicalLayer, 4, (byte)testBridgeRequest.TelemetryRatio, (CtmBeepEnables)testBridgeRequest.BeepEnables))
                .Returns(ManagerConnectStatus.FailedConnect);

            // Use the mock framework to raise a connection status event
            Thread threadedEvent = new Thread(() =>
            {
                mockDevice.Raise(m => m.UnexpectedCtmDisconnectHandler += null, new EventArgs());
            });
            threadedEvent.Start();

            // Read the event out of the client stream and check for CTM disconnected notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read first message");
            ConnectionUpdate theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "CTM Disconnected!", "Incorrect status in first message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in first message");

            // Read the event out of the client stream and check for CTM disposed notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read second message");
            theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "CTM Disposed!", "Incorrect status in second message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in second message");

            // Read the event out of the client stream and check for CTM connection failed notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read third message");
            theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "CTM Retry Failed! 3 tries remaining.", "Incorrect status in third message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in third message");

            // Read the event out of the client stream and check for CTM connection failed notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read fourth message");
            theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "CTM Retry Failed! 2 tries remaining.", "Incorrect status in fourth message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in fourth message");
            
            // Read the event out of the client stream and check for CTM connection failed notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read fifth message");
            theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "CTM Retry Failed! 1 tries remaining.", "Incorrect status in fifth message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in fifth message");
            
            // Read the event out of the client stream and check for CTM connection failed notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read sixth message");
            theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "CTM Retry Failed! 0 tries remaining.", "Incorrect status in sixth message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in sixth message");

            // Read the event out of the client stream and check for CTM connection failed notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read seventh message");
            theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "SummitSystem Reconnection Failed!", "Incorrect status in seventh message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in seventh message");

            // Wait until the threading disposal is complete and then close out the server
            threadedEvent.Join();
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.5 - Connection Monitoring (updates sent to client)
        [Test]
        public async Task ConnectionMonitoring_DisconnectionEventReceived_ReconnectionINSAttemptsExceeded()
        {
            // This test evaluates the OMNI connection stream responding with the correct behavior when the CTM is disconnected and unable to be reconnected to by OMNI due to an unreconnectable INS
            // Expected behavior of SummitSystem Object:
            // - Summit API indicates disconnection occuring by calling event handler, returns "NoCtmConnected" return codes while reconnecting.
            // - Summit API indicates failure in API-reconnect by returning API commands with "IsDisposed"
            // Expected behavior of OMNI:
            // - Reports CTM as disconnected
            // - polls Summit API until "NoError" is returned
            // - Reports CTM is disposed
            // - attempts to reconnect 3 times (defined by retry parameter)
            // - each attempt fails with an appropriate message on connection monitoring stream indicating continued failure to connect to INS
            // - after 3 tries, stream indicates end of attempts and that reconnection has failed

            // First, create a summit service with a cached connected device
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            ConnectBridgeRequest testBridgeRequest = new ConnectBridgeRequest() { Retries = 3, PhysicalLayer = SummitPhysicalLayer.Any, TelemetryRatio = 8, TelemetryMode = "4", BeepEnables = SummitBeepConfig.DeviceDiscovered };
            repository.CacheConnection("//summit/bridge/foo", mockDevice.Object, mockManager.Object, testBridgeRequest, new InstrumentInfo()); ;
            repository.GetConnectionByName("//summit/bridge/foo").SetDeviceSerial("bar");
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connects to the service and initiates streaming, ensures that it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new OpenMind.BridgeManagerService.BridgeManagerServiceClient(channel);
            var stream = aBridgeManagerClient.ConnectionStatusStream(new SetStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.ConnectionStatusQueue == null)
            {
                await Task.Delay(100);
                Assert.Less(count++, 10, "Initial Connect Timeout");
            };

            // After first battery poll Summit should indicate it is in a disposed state for this test
            BatteryStatusResult theBatteryStatusResult;
            mockDevice.SetupSequence(Device => Device.ReadBatteryLevel(out theBatteryStatusResult))
                .Returns(new APIReturnInfo(APIRejectCodes.NoCtmConnected, 0, DateTime.Now, DateTime.Now, 0))    // Initial Battery poll while API is still attempting to reconnect
                .Returns(new APIReturnInfo(APIRejectCodes.IsDisposed, 0, DateTime.Now, DateTime.Now, 0));       // Battery poll when API is done reconnect
            mockDevice.SetupSequence(Device => Device.IsDisposed)
                .Returns(false)                                                                                 // API is still attempting to reconnect
                .Returns(true);                                                                                 // API is done reconnect
            var newMockDevice = new Mock<ISummitSystem>();
            ISummitSystem reconnectedSummit = newMockDevice.Object;
            mockManager.Setup(Manager => Manager.CreateSummit(out reconnectedSummit, new InstrumentInfo(), (InstrumentPhysicalLayers)testBridgeRequest.PhysicalLayer, 4, (byte)testBridgeRequest.TelemetryRatio, (CtmBeepEnables)testBridgeRequest.BeepEnables))
                .Returns(ManagerConnectStatus.Success);
            ConnectReturn aConnectWarnings;
            newMockDevice.Setup(Device => Device.StartInsSession(null, out aConnectWarnings, true))
                .Returns(new APIReturnInfo(InstrumentReturnCode.Error, CommandIds.StartDeviceSession, DateTime.Now, DateTime.Now, 0));

            // Use the mock framework to raise a connection status event
            Thread threadedEvent = new Thread(() =>
            {
                mockDevice.Raise(m => m.UnexpectedCtmDisconnectHandler += null, new EventArgs());
            });
            threadedEvent.Start();

            // Read the event out of the client stream and check for CTM disconnected notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read first message");
            ConnectionUpdate theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "CTM Disconnected!", "Incorrect status in first message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in first message");

            // Read the event out of the client stream and check for CTM disposed notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read second message");
            theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "CTM Disposed!", "Incorrect status in second message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in second message");

            // Read the event out of the client stream and check for CTM connection failed notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read third message");
            theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "INS Retry Failed! 2 tries remaining.", "Incorrect status in third message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in third message");

            // Read the event out of the client stream and check for CTM connection failed notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read fourth message");
            theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "INS Retry Failed! 1 tries remaining.", "Incorrect status in fourth message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in fourth message");

            // Read the event out of the client stream and check for CTM connection failed notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read fifth message");
            theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "INS Retry Failed! 0 tries remaining.", "Incorrect status in fifth message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in fifth message");

            // Read the event out of the client stream and check for CTM connection failed notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read sixth message");
            theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "SummitSystem Reconnection Failed!", "Incorrect status in sixth message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in sixth message");

            // Wait until the threading disposal is complete and then close out the server
            threadedEvent.Join();
            aTestServer.StopServer();
        }

        // Tests Requirement 5.2.5 - Connection Monitoring (updates sent to client)
        [Test]
        public async Task ConnectionMonitoring_DisconnectionEventReceived_ReconnectionSucceeded()
        {
            // This test evaluates the OMNI connection stream responding with the correct behavior when the CTM is disconnected and is able to be reconnected to by OMNI
            // Expected behavior of SummitSystem Object:
            // - Summit API indicates disconnection occuring by calling event handler, returns "NoCtmConnected" return codes while reconnecting.
            // - Summit API indicates failure in API-reconnect by returning API commands with "IsDisposed"
            // Expected behavior of OMNI:
            // - Reports CTM as disconnected
            // - polls Summit API until "NoError" is returned
            // - Reports CTM is disposed
            // - attempts to reconnect 3 times (defined by retry parameter)
            // - in first attempt, just for flavor, the CTM connection fails
            // - in second attempt, both CTM and INS connections succeed.
            // - Reports via connection stream that reconnection is successful

            // First, create a summit service with a cached connected device
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            ConnectBridgeRequest testBridgeRequest = new ConnectBridgeRequest() { Retries = 3, PhysicalLayer = SummitPhysicalLayer.Any, TelemetryRatio = 8, TelemetryMode = "4", BeepEnables = SummitBeepConfig.DeviceDiscovered };
            repository.CacheConnection("//summit/bridge/foo", mockDevice.Object, mockManager.Object, testBridgeRequest, new InstrumentInfo()); ;
            repository.GetConnectionByName("//summit/bridge/foo").SetDeviceSerial("bar");
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connects to the service and initiates streaming, ensures that it's running (queue != null) before proceeding
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.BridgeManagerService.BridgeManagerServiceClient aBridgeManagerClient = new OpenMind.BridgeManagerService.BridgeManagerServiceClient(channel);
            var stream = aBridgeManagerClient.ConnectionStatusStream(new SetStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.ConnectionStatusQueue == null)
            {
                await Task.Delay(100);
                Assert.Less(count++, 10, "Initial Connect Timeout");
            };

            // After first battery poll Summit should indicate it is in a disposed state for this test
            BatteryStatusResult theBatteryStatusResult;
            mockDevice.SetupSequence(Device => Device.ReadBatteryLevel(out theBatteryStatusResult))
                .Returns(new APIReturnInfo(APIRejectCodes.NoCtmConnected, 0, DateTime.Now, DateTime.Now, 0))    // Initial Battery poll while API is still attempting to reconnect
                .Returns(new APIReturnInfo(APIRejectCodes.IsDisposed, 0, DateTime.Now, DateTime.Now, 0));       // Battery poll when API is done reconnect
            mockDevice.SetupSequence(Device => Device.IsDisposed)
                .Returns(false)                                                                                 // API is still attempting to reconnect
                .Returns(true);                                                                                 // API is done reconnect
            var newMockDevice = new Mock<ISummitSystem>();
            ISummitSystem reconnectedSummit = newMockDevice.Object;
            mockManager.SetupSequence(Manager => Manager.CreateSummit(out reconnectedSummit, new InstrumentInfo(), (InstrumentPhysicalLayers)testBridgeRequest.PhysicalLayer, 4, (byte)testBridgeRequest.TelemetryRatio, (CtmBeepEnables)testBridgeRequest.BeepEnables))
                .Returns(ManagerConnectStatus.FailedConnect)
                .Returns(ManagerConnectStatus.Success);
            ConnectReturn aConnectWarnings;
            newMockDevice.SetupSequence(Device => Device.StartInsSession(null, out aConnectWarnings, true))
                .Returns(new APIReturnInfo(InstrumentReturnCode.Success, CommandIds.StartDeviceSession, DateTime.Now, DateTime.Now, 0));

            // Use the mock framework to raise a connection status event
            Thread threadedEvent = new Thread(() =>
            {
                mockDevice.Raise(m => m.UnexpectedCtmDisconnectHandler += null, new EventArgs());
            });
            threadedEvent.Start();

            // Read the event out of the client stream and check for CTM disconnected notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read first message");
            ConnectionUpdate theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "CTM Disconnected!", "Incorrect status in first message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in first message");

            // Read the event out of the client stream and check for CTM disposed notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read second message");
            theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "CTM Disposed!", "Incorrect status in second message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in second message");

            // Read the event out of the client stream and check for CTM connection failed notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read third message");
            theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "CTM Retry Failed! 2 tries remaining.", "Incorrect status in third message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in third message");

            // Read the event out of the client stream and check for CTM connection failed notification
            Assert.IsTrue(await stream.ResponseStream.MoveNext(), "Unable to read fourth message");
            theReceivedUpdate = stream.ResponseStream.Current;
            Assert.IsTrue(theReceivedUpdate.ConnectionStatus == "SummitSystem Reconnected!", "Incorrect status in fourth message");
            Assert.IsTrue(theReceivedUpdate.Name == "//summit/bridge/foo/device/bar", "Incorrect name in fourth message");

            // Wait until the threading disposal is complete and then close out the server
            threadedEvent.Join();
            aTestServer.StopServer();
        }

        #endregion Reconnection Logic Tests

        #region Multiple Active Connection Tests
        // Tests Requirement 6.1.1 - Multiple clients
        [Test]
        public void MultipleConnections_TwoClientOneDevice_ConnectionRequestsSuccess()
        {
            // First, create a summit service with a cached device
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            repository.CacheConnection("//summit/bridge/foo", mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            repository.GetConnectionByName("//summit/bridge/foo").SetDeviceSerial("bar");
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a mock response to respond to device status requests
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

            // Create "A" client that connect to the service and requests the device's status
            Channel channelA = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientA = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelA);
            var statusA = aDeviceManagerClientA.DeviceStatus(new DeviceStatusRequest() { Name = "//summit/bridge/foo/device/bar" });

            // Create a second "B" client that connect to the service and requests the device's status
            Channel channelB = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientB = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelB);
            var statusB = aDeviceManagerClientB.DeviceStatus(new DeviceStatusRequest() { Name = "//summit/bridge/foo/device/bar" });

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that both device status calls succeed, which shows that multiple clients can utilize the gRPC service simultaneously
            Assert.IsFalse (statusA == null, "Client A Device Status is Null");
            Assert.IsTrue(statusA.Name == "//summit/bridge/foo/device/bar", "Client A Name Mismatch");
            Assert.IsFalse(statusB == null, "Client B Device Status is Null");
            Assert.IsTrue(statusB.Name == "//summit/bridge/foo/device/bar", "Client B Name Mismatch");
        }

        // Tests Requirement 6.1.1 - Multiple clients
        // Tests Requirement 6.2.1 - Multiple devices
        [Test]
        public void MultipleConnections_TwoClientTwoDevices_ConnectionRequestsSuccess()
        {
            // First, create a summit service with two cached devices
            var mockManager = new Mock<ISummitManager>();
            var mockDeviceA = new Mock<ISummitSystem>();
            var mockDeviceB = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            repository.CacheConnection("//summit/bridge/fooA", mockDeviceA.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            repository.CacheConnection("//summit/bridge/fooB", mockDeviceB.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            repository.GetConnectionByName("//summit/bridge/fooA").SetDeviceSerial("barA");
            repository.GetConnectionByName("//summit/bridge/fooB").SetDeviceSerial("barB");
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a mock response to respond to device status requests
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
            mockDeviceA.Setup(Device => Device.ReadBatteryLevel(out batteryStatusSuccess))
                .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));
            mockDeviceB.Setup(Device => Device.ReadBatteryLevel(out batteryStatusSuccess))
                .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));

            // Create "A" client that connect to the service and requests the device's status
            Channel channelA = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientA = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelA);
            var statusA = aDeviceManagerClientA.DeviceStatus(new DeviceStatusRequest() { Name = "//summit/bridge/fooA/device/barA" });

            // Create a second "B" client that connect to the service and requests the device's status
            Channel channelB = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientB = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelB);
            var statusB = aDeviceManagerClientB.DeviceStatus(new DeviceStatusRequest() { Name = "//summit/bridge/fooB/device/barB" });

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that both device status calls succeed, which shows that multiple clients can utilize the gRPC service simultaneously to communitcate with two devices
            Assert.IsFalse(statusA == null, "Client A Device Status is Null");
            Assert.IsTrue(statusA.Name == "//summit/bridge/fooA/device/barA", "Client A Name Mismatch");
            Assert.IsFalse(statusB == null, "Client B Device Status is Null");
            Assert.IsTrue(statusB.Name == "//summit/bridge/fooB/device/barB", "Client B Name Mismatch");
        }

        // Tests Requirement 6.2.1 - Multiple devices
        [Test]
        public void MultipleConnections_OneClientTwoDevices_ConnectionRequestsSuccess()
        {
            // First, create a summit service with two cached devices
            var mockManager = new Mock<ISummitManager>();
            var mockDeviceA = new Mock<ISummitSystem>();
            var mockDeviceB = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            repository.CacheConnection("//summit/bridge/fooA", mockDeviceA.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            repository.CacheConnection("//summit/bridge/fooB", mockDeviceB.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            repository.GetConnectionByName("//summit/bridge/fooA").SetDeviceSerial("barA");
            repository.GetConnectionByName("//summit/bridge/fooB").SetDeviceSerial("barB");
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a mock response to respond to device status requests
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
            mockDeviceA.Setup(Device => Device.ReadBatteryLevel(out batteryStatusSuccess))
                .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));
            mockDeviceB.Setup(Device => Device.ReadBatteryLevel(out batteryStatusSuccess))
                .Returns(new APIReturnInfo(APIRejectCodes.NoError, 0, DateTime.Now, DateTime.Now, 0));

            // Create "A" client that connect to the service and requests the device's status
            Channel channelA = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientA = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelA);
            var statusA = aDeviceManagerClientA.DeviceStatus(new DeviceStatusRequest() { Name = "//summit/bridge/fooA/device/barA" });
            var statusB = aDeviceManagerClientA.DeviceStatus(new DeviceStatusRequest() { Name = "//summit/bridge/fooB/device/barB" });

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that both device status calls succeed, which shows that multiple clients can utilize the gRPC service simultaneously to communitcate with two devices
            Assert.IsFalse(statusA == null, "Client Device A Status is Null");
            Assert.IsTrue(statusA.Name == "//summit/bridge/fooA/device/barA", "Device A Name Mismatch");
            Assert.IsFalse(statusB == null, "Client Device B Status is Null");
            Assert.IsTrue(statusB.Name == "//summit/bridge/fooB/device/barB", "Device B Name Mismatch");
        }
        #endregion Multiple Active Connection Tests
    }
}

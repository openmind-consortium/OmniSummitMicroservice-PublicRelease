using Grpc.Core;
using Medtronic.NeuroStim.Olympus.DataTypes.Sensing;
using Medtronic.NeuroStim.Olympus.DataTypes.Sensing.Packets;
using Medtronic.SummitAPI.Events;
using Medtronic.TelemetryM;
using Moq;
using NUnit.Framework;
using OpenMind;
using OpenMindServer.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenMindServer.UnitTests
{
    [TestFixture]
    public partial class SummitServiceTest
    {
        #region Time Domain Streaming Tests
        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateTimeDomainDataStream_NoCachedConnection_ReturnsEmpty()
        {
            // First, create a summit service
            var mockManager = new Mock<ISummitManager>();
            var repository = Repository.GetRepositoryInstance();
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connects to the service.
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);

            // Attempt to connect to the stream when there is no device connected
            var stream = aDeviceManagerClient.TimeDomainStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo" });

            // Save test values for later asserts
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            bool streamStatusCode = stream.GetStatus().StatusCode == StatusCode.OK;

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that the returned stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext, "Move Next returned true, expected false");
            Assert.IsTrue(streamStatusCode, "Get status was not 'OK'");
        }

        // Tests Requirement 5.2.9 - Data Streaming
        // Tests Requirement 6.1.2 - Only stream to one client
        [Test]
        public async Task CreateTimeDomainDataStream_AlreadyConnected_ReturnsEmpty()
        {
            // First, create a summit service with a cached connected device
            var mockManager = new Mock<ISummitManager>();
            var mockDevice = new Mock<ISummitSystem>();
            var repository = Repository.GetRepositoryInstance();
            repository.CacheConnection("//summit/bridge/foo", mockDevice.Object, mockManager.Object, new ConnectBridgeRequest(), new InstrumentInfo());
            repository.GetConnectionByName("//summit/bridge/foo").SetDeviceSerial("bar");
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository,summitService);
            aTestServer.StartServer();

            // Create "A" client that connect to the service and initiates streaming, ensure that the it's running (queue != null) before proceeding
            Channel channelA = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientA = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelA);
            var streamA = aDeviceManagerClientA.TimeDomainStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar"});
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.TimeDomainDataQueue == null && count < 10)
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientB = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelB);
            var streamB = aDeviceManagerClientB.TimeDomainStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });

            // Save test values for later asserts
            bool streamMoveNext = await streamB.ResponseStream.MoveNext();
            bool streamStatusCode = streamB.GetStatus().StatusCode == StatusCode.OK;

            // Failed to connect, leave test rig in correct form by closing out the server
            aTestServer.StopServer();

            // Expected behavior is that the returned stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext, "Move Next returned true, expected false");
            Assert.IsTrue(streamStatusCode, "Get status was not 'OK'");
        }

        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateTimeDomainDataStream_EnableStreamRequest_ReturnsStream()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);
            var stream = aDeviceManagerClient.TimeDomainStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.TimeDomainDataQueue == null && count < 10)
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

            // Create the fake event data
            List<short> channel0data = new List<short>() { 0, 1, 2, 3, 4 };
            List<short> channel1data = new List<short>() { 5, 6, 7, 8, 9 };
            List<short> channel2data = new List<short>() { 10, 11, 12, 13, 14 };
            List<short> channel3data = new List<short>() { 15, 16, 17, 18, 19 };
            
            Dictionary<SenseTimeDomainChannel, List<short>> ChannelSamples = new Dictionary<SenseTimeDomainChannel, List<short>>();
            ChannelSamples.Add(SenseTimeDomainChannel.Ch0, channel0data);
            ChannelSamples.Add(SenseTimeDomainChannel.Ch1, channel1data);
            ChannelSamples.Add(SenseTimeDomainChannel.Ch2, channel2data);
            ChannelSamples.Add(SenseTimeDomainChannel.Ch3, channel3data);

            List<EvokedResponseMarker> channel0stim = new List<EvokedResponseMarker>() { new EvokedResponseMarker(1, 2), new EvokedResponseMarker(2, 3) };
            List<EvokedResponseMarker> channel1stim = new List<EvokedResponseMarker>() { new EvokedResponseMarker(3, 4), new EvokedResponseMarker(4, 5) };
            List<EvokedResponseMarker> channel2stim = new List<EvokedResponseMarker>() { new EvokedResponseMarker(5, 6), new EvokedResponseMarker(6, 7) };
            List<EvokedResponseMarker> channel3stim = new List<EvokedResponseMarker>() { new EvokedResponseMarker(7, 8), new EvokedResponseMarker(8, 9) };

            Dictionary<SenseTimeDomainChannel, List<EvokedResponseMarker>> EvokedSamples = new Dictionary<SenseTimeDomainChannel, List<EvokedResponseMarker>>();
            EvokedSamples.Add(SenseTimeDomainChannel.Ch0, channel0stim);
            EvokedSamples.Add(SenseTimeDomainChannel.Ch1, channel1stim);
            EvokedSamples.Add(SenseTimeDomainChannel.Ch2, channel2stim);
            EvokedSamples.Add(SenseTimeDomainChannel.Ch3, channel3stim);

            SensingTimeDomainPacket aNewTDPacket = new SensingTimeDomainPacket();
            aNewTDPacket.ChannelSamples = ChannelSamples;
            aNewTDPacket.EvokedResponseMarkers = EvokedSamples;
            aNewTDPacket.SampleRate = TdSampleRates.Sample0250Hz;
            aNewTDPacket.Header.DataTypeSequence = 1;
            aNewTDPacket.Header.GlobalSequence = 2;
            aNewTDPacket.Header.SystemTick = 3;
            aNewTDPacket.Header.Timestamp = new Medtronic.NeuroStim.Olympus.DataTypes.Core.TimeOfDay(4);
            
            Dictionary<SenseTimeDomainChannel, double> ChannelScalars = new Dictionary<SenseTimeDomainChannel, double>();
            ChannelScalars.Add(SenseTimeDomainChannel.Ch0, 1);
            ChannelScalars.Add(SenseTimeDomainChannel.Ch1, 1);
            ChannelScalars.Add(SenseTimeDomainChannel.Ch2, 1);
            ChannelScalars.Add(SenseTimeDomainChannel.Ch3, 1);

            DateTime genTimeTestValue = DateTime.Now.Subtract(new TimeSpan(0,0,1));
            DateTime rxTimeTestValue = DateTime.Now;
            SensingEventTD aNewTDEvent = new SensingEventTD(ChannelScalars, aNewTDPacket, genTimeTestValue, rxTimeTestValue);

            // Use the mock framework to raise a time domain event in the 
            mockDevice.Raise(m => m.DataReceivedTDHandler += null, aNewTDEvent);

            // Read the event out of the client stream and check values
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            TimeDomainUpdate theReceivedUpdate = stream.ResponseStream.Current;

            // Close out the server
            aTestServer.StopServer();

            // Check asserts for expected values
            Assert.IsTrue(streamMoveNext, "Move Next returned false, expected true");

            Assert.AreEqual(theReceivedUpdate.Data[0].ChannelData.ToList(), channel0data);
            Assert.AreEqual(theReceivedUpdate.Data[0].StimulationPulseIndeces[0].DataIndex, channel0stim[0].IndexOfDataPointFollowing);
            Assert.AreEqual(theReceivedUpdate.Data[0].StimulationPulseIndeces[0].TimeBeforeInMicroseconds, channel0stim[0].TimeBeforeDataPointInMicroSeconds);
            Assert.AreEqual(theReceivedUpdate.Data[0].StimulationPulseIndeces[1].DataIndex, channel0stim[1].IndexOfDataPointFollowing);
            Assert.AreEqual(theReceivedUpdate.Data[0].StimulationPulseIndeces[1].TimeBeforeInMicroseconds, channel0stim[1].TimeBeforeDataPointInMicroSeconds);
            Assert.AreEqual(theReceivedUpdate.Data[0].ChannelId, SenseTimeDomainChannel.Ch0.ToString("g"));
            Assert.AreEqual(theReceivedUpdate.Data[0].SamplingRate, 250);
            Assert.AreEqual(theReceivedUpdate.Data[0].Units, "millivolts");

            Assert.AreEqual(theReceivedUpdate.Data[1].ChannelData.ToList(), channel1data);
            Assert.AreEqual(theReceivedUpdate.Data[1].StimulationPulseIndeces[0].DataIndex, channel1stim[0].IndexOfDataPointFollowing);
            Assert.AreEqual(theReceivedUpdate.Data[1].StimulationPulseIndeces[0].TimeBeforeInMicroseconds, channel1stim[0].TimeBeforeDataPointInMicroSeconds);
            Assert.AreEqual(theReceivedUpdate.Data[1].StimulationPulseIndeces[1].DataIndex, channel1stim[1].IndexOfDataPointFollowing);
            Assert.AreEqual(theReceivedUpdate.Data[1].StimulationPulseIndeces[1].TimeBeforeInMicroseconds, channel1stim[1].TimeBeforeDataPointInMicroSeconds);
            Assert.AreEqual(theReceivedUpdate.Data[1].ChannelId, SenseTimeDomainChannel.Ch1.ToString("g"));
            Assert.AreEqual(theReceivedUpdate.Data[1].SamplingRate, 250);
            Assert.AreEqual(theReceivedUpdate.Data[1].Units, "millivolts");

            Assert.AreEqual(theReceivedUpdate.Data[2].ChannelData.ToList(), channel2data);
            Assert.AreEqual(theReceivedUpdate.Data[2].StimulationPulseIndeces[0].DataIndex, channel2stim[0].IndexOfDataPointFollowing);
            Assert.AreEqual(theReceivedUpdate.Data[2].StimulationPulseIndeces[0].TimeBeforeInMicroseconds, channel2stim[0].TimeBeforeDataPointInMicroSeconds);
            Assert.AreEqual(theReceivedUpdate.Data[2].StimulationPulseIndeces[1].DataIndex, channel2stim[1].IndexOfDataPointFollowing);
            Assert.AreEqual(theReceivedUpdate.Data[2].StimulationPulseIndeces[1].TimeBeforeInMicroseconds, channel2stim[1].TimeBeforeDataPointInMicroSeconds);
            Assert.AreEqual(theReceivedUpdate.Data[2].ChannelId, SenseTimeDomainChannel.Ch2.ToString("g"));
            Assert.AreEqual(theReceivedUpdate.Data[2].SamplingRate, 250);
            Assert.AreEqual(theReceivedUpdate.Data[2].Units, "millivolts");

            Assert.AreEqual(theReceivedUpdate.Data[3].ChannelData.ToList(), channel3data);
            Assert.AreEqual(theReceivedUpdate.Data[3].StimulationPulseIndeces[0].DataIndex, channel3stim[0].IndexOfDataPointFollowing);
            Assert.AreEqual(theReceivedUpdate.Data[3].StimulationPulseIndeces[0].TimeBeforeInMicroseconds, channel3stim[0].TimeBeforeDataPointInMicroSeconds);
            Assert.AreEqual(theReceivedUpdate.Data[3].StimulationPulseIndeces[1].DataIndex, channel3stim[1].IndexOfDataPointFollowing);
            Assert.AreEqual(theReceivedUpdate.Data[3].StimulationPulseIndeces[1].TimeBeforeInMicroseconds, channel3stim[1].TimeBeforeDataPointInMicroSeconds);
            Assert.AreEqual(theReceivedUpdate.Data[3].ChannelId, SenseTimeDomainChannel.Ch3.ToString("g"));
            Assert.AreEqual(theReceivedUpdate.Data[3].SamplingRate, 250);
            Assert.AreEqual(theReceivedUpdate.Data[3].Units, "millivolts");

            Assert.AreEqual(theReceivedUpdate.Header.DataTypeSequenceNumber, 1);
            Assert.AreEqual(theReceivedUpdate.Header.GlobalSequenceNumber, 2);
            Assert.AreEqual(theReceivedUpdate.Header.InsTick, 3);
            Assert.AreEqual(theReceivedUpdate.Header.InsTimestamp, 4);
            Assert.AreEqual(theReceivedUpdate.Header.SystemEstDeviceTxTime, genTimeTestValue.ToFileTimeUtc());
            Assert.AreEqual(theReceivedUpdate.Header.SystemRxTime, rxTimeTestValue.ToFileTimeUtc());
        }

        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateTimeDomainDataStream_ConnectedDisableStream_EndsStream()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);
            var stream = aDeviceManagerClient.TimeDomainStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.TimeDomainDataQueue == null && count < 10)
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
            aDeviceManagerClient.TimeDomainStream(new SetDataStreamEnable() { EnableStream = false, Name = "//summit/bridge/foo/device/bar" });

            // Read the event out of the client stream and check values
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            bool streamStatusCode = stream.GetStatus().StatusCode == StatusCode.OK;

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that the closed stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext);
            Assert.IsTrue(streamStatusCode);
        }
        #endregion Time Domain Streaming Tests

        #region Fourier Transform Streaming Tests
        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateFourierDataStream_NoCachedConnection_ReturnsEmpty()
        {
            // First, create a summit service
            var mockManager = new Mock<ISummitManager>();
            var repository = Repository.GetRepositoryInstance();
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connects to the service.
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);

            // Attempt to connect to the stream when there is no device connected
            var stream = aDeviceManagerClient.FourierTransformStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo" });

            // Save test values for later asserts
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            bool streamStatusCode = stream.GetStatus().StatusCode == StatusCode.OK;

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that the returned stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext, "Move Next returned true, expected false");
            Assert.IsTrue(streamStatusCode, "Get status was not 'OK'");
        }

        // Tests Requirement 5.2.9 - Data Streaming
        // Tests Requirement 6.1.2 - Only stream to one client
        [Test]
        public async Task CreateFourierDataStream_AlreadyConnected_ReturnsEmpty()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientA = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelA);
            var streamA = aDeviceManagerClientA.FourierTransformStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.FourierDataQueue == null && count < 10)
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientB = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelB);
            var streamB = aDeviceManagerClientB.FourierTransformStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });

            // Save test values for later asserts
            bool streamMoveNext = await streamB.ResponseStream.MoveNext();
            bool streamStatusCode = streamB.GetStatus().StatusCode == StatusCode.OK;

            // Failed to connect, leave test rig in correct form by closing out the server
            aTestServer.StopServer();

            // Expected behavior is that the returned stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext, "Move Next returned true, expected false");
            Assert.IsTrue(streamStatusCode, "Get status was not 'OK'");
        }

        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateFourierDataStream_EnableStreamRequestData_ReturnsStream()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);
            var stream = aDeviceManagerClient.FourierTransformStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.FourierDataQueue == null && count < 10)
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

            // Create the fake event data
            List<ushort> channeldata = new List<ushort>() { 0, 1, 2, 3, 4 };

            SensingFftPacket aNewFFTPacket = new SensingFftPacket();
            aNewFFTPacket.Header.DataTypeSequence = 1;
            aNewFFTPacket.Header.GlobalSequence = 2;
            aNewFFTPacket.Header.SystemTick = 3;
            aNewFFTPacket.Header.Timestamp = new Medtronic.NeuroStim.Olympus.DataTypes.Core.TimeOfDay(4);
            aNewFFTPacket.Bins = channeldata;
            aNewFFTPacket.Channel = SenseTimeDomainChannel.Ch0;
            aNewFFTPacket.FftSize = FftSizes.Size0064;
            aNewFFTPacket.SampleRate = TdSampleRates.Sample0250Hz;


            Dictionary<SenseTimeDomainChannel, double> ChannelScalars = new Dictionary<SenseTimeDomainChannel, double>();
            ChannelScalars.Add(SenseTimeDomainChannel.Ch0, 1);
            ChannelScalars.Add(SenseTimeDomainChannel.Ch1, 1);
            ChannelScalars.Add(SenseTimeDomainChannel.Ch2, 1);
            ChannelScalars.Add(SenseTimeDomainChannel.Ch3, 1);

            DateTime genTimeTestValue = DateTime.Now.Subtract(new TimeSpan(0, 0, 1));
            DateTime rxTimeTestValue = DateTime.Now;
            SensingEventFFT aNewFourierEvent = new SensingEventFFT(ChannelScalars, aNewFFTPacket, genTimeTestValue, rxTimeTestValue);

            // Use the mock framework to raise a time domain event in the 
            mockDevice.Raise(m => m.DataReceivedFFTHandler += null, aNewFourierEvent);

            // Read the event out of the client stream and check values
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            FourierTransformUpdate theReceivedUpdate = stream.ResponseStream.Current;

            // Close out the server
            aTestServer.StopServer();

            // Check asserts for expected values
            Assert.IsTrue(streamMoveNext, "Move Next returned false, expected true");
            Assert.AreEqual(theReceivedUpdate.Name, "//summit/bridge/foo/device/bar");
            Assert.AreEqual(theReceivedUpdate.Header.DataTypeSequenceNumber, 1);
            Assert.AreEqual(theReceivedUpdate.Header.GlobalSequenceNumber, 2);
            Assert.AreEqual(theReceivedUpdate.Header.InsTick, 3);
            Assert.AreEqual(theReceivedUpdate.Header.InsTimestamp, 4);
            Assert.AreEqual(theReceivedUpdate.Header.SystemEstDeviceTxTime, genTimeTestValue.ToFileTimeUtc());
            Assert.AreEqual(theReceivedUpdate.Header.SystemRxTime, rxTimeTestValue.ToFileTimeUtc());

            Assert.AreEqual(theReceivedUpdate.Data[0].ChannelData, channeldata);
            Assert.AreEqual(theReceivedUpdate.Data[0].ChannelId, SenseTimeDomainChannel.Ch0.ToString("g"));
            Assert.AreEqual(theReceivedUpdate.Data[0].FourierLength, 64);
            Assert.AreEqual(theReceivedUpdate.Data[0].SamplingRate, 250);
            Assert.AreEqual(theReceivedUpdate.Data[0].Units, string.Empty);
        }

        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateFourierDataStream_ConnectedDisableStream_EndsStream()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);
            var stream = aDeviceManagerClient.FourierTransformStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.FourierDataQueue == null && count < 10)
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
            aDeviceManagerClient.FourierTransformStream(new SetDataStreamEnable() { EnableStream = false, Name = "//summit/bridge/foo/device/bar" });

            // Read the event out of the client stream and check values
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            bool streamStatusCode = stream.GetStatus().StatusCode == StatusCode.OK;

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that the closed stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext);
            Assert.IsTrue(streamStatusCode);
        }
        #endregion Fourier Transform Streaming Tests

        #region Power Domain Streaming Tests
        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateBandPowerDataStream_NoCachedConnection_ReturnsEmpty()
        {
            // First, create a summit service
            var mockManager = new Mock<ISummitManager>();
            var repository = Repository.GetRepositoryInstance();
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository,summitService);
            aTestServer.StartServer();

            // Create a client that connects to the service.
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);

            // Attempt to connect to the stream when there is no device connected
            var stream = aDeviceManagerClient.BandPowerStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo" });

            // Save test values for later asserts
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            bool streamStatusCode = stream.GetStatus().StatusCode == StatusCode.OK;

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that the returned stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext, "Move Next returned true, expected false");
            Assert.IsTrue(streamStatusCode, "Get status was not 'OK'");
        }

        // Tests Requirement 5.2.9 - Data Streaming
        // Tests Requirement 6.1.2 - Only stream to one client
        [Test]
        public async Task CreateBandPowerDataStream_AlreadyConnected_ReturnsEmpty()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientA = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelA);
            var streamA = aDeviceManagerClientA.BandPowerStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.BandPowerDataQueue == null && count < 10)
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientB = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelB);
            var streamB = aDeviceManagerClientB.BandPowerStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });

            // Save test values for later asserts
            bool streamMoveNext = await streamB.ResponseStream.MoveNext();
            bool streamStatusCode = streamB.GetStatus().StatusCode == StatusCode.OK;

            // Failed to connect, leave test rig in correct form by closing out the server
            aTestServer.StopServer();

            // Expected behavior is that the returned stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext, "Move Next returned true, expected false");
            Assert.IsTrue(streamStatusCode, "Get status was not 'OK'");
        }

        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateBandPowerDataStream_EnableStreamRequestData_ReturnsStream()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);
            var stream = aDeviceManagerClient.BandPowerStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.BandPowerDataQueue == null && count < 10)
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

            // Create the fake event data
            List<uint> packetBands = new List<uint>() { 0, 30 };

            SensingPowerPacket aNewPowerPacket = new SensingPowerPacket();
            aNewPowerPacket.Header.DataTypeSequence = 1;
            aNewPowerPacket.Header.GlobalSequence = 2;
            aNewPowerPacket.Header.SystemTick = 3;
            aNewPowerPacket.Header.Timestamp = new Medtronic.NeuroStim.Olympus.DataTypes.Core.TimeOfDay(4);
            aNewPowerPacket.Bands = packetBands;
            aNewPowerPacket.FftSize = FftSizes.Size0064;
            aNewPowerPacket.SampleRate = TdSampleRates.Sample0250Hz;


            DateTime genTimeTestValue = DateTime.Now.Subtract(new TimeSpan(0, 0, 1));
            DateTime rxTimeTestValue = DateTime.Now;
            SensingEventPower aNewBandPowerEvent = new SensingEventPower(aNewPowerPacket, genTimeTestValue, rxTimeTestValue);

            // Use the mock framework to raise a time domain event in the 
            mockDevice.Raise(m => m.DataReceivedPowerHandler += null, aNewBandPowerEvent);

            // Read the event out of the client stream and check values
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            BandPowerUpdate theReceivedUpdate = stream.ResponseStream.Current;

            // Close out the server
            aTestServer.StopServer();

            // Check asserts for expected values
            Assert.IsTrue(streamMoveNext, "Move Next returned false, expected true");

            Assert.AreEqual(theReceivedUpdate.Name, "//summit/bridge/foo/device/bar");
            Assert.AreEqual(theReceivedUpdate.Header.DataTypeSequenceNumber, 1);
            Assert.AreEqual(theReceivedUpdate.Header.GlobalSequenceNumber, 2);
            Assert.AreEqual(theReceivedUpdate.Header.InsTick, 3);
            Assert.AreEqual(theReceivedUpdate.Header.InsTimestamp, 4);
            Assert.AreEqual(theReceivedUpdate.Header.SystemEstDeviceTxTime, genTimeTestValue.ToFileTimeUtc());
            Assert.AreEqual(theReceivedUpdate.Header.SystemRxTime, rxTimeTestValue.ToFileTimeUtc());

            Assert.AreEqual(theReceivedUpdate.Data[0].ChannelData, packetBands[0]);
            Assert.AreEqual(theReceivedUpdate.Data[0].ChannelId, "0");
            Assert.AreEqual(theReceivedUpdate.Data[0].FourierLength, 64);
            Assert.AreEqual(theReceivedUpdate.Data[0].SamplingRate, 250);
            Assert.AreEqual(theReceivedUpdate.Data[0].Units, string.Empty);

            Assert.AreEqual(theReceivedUpdate.Data[1].ChannelData, packetBands[1]);
            Assert.AreEqual(theReceivedUpdate.Data[1].ChannelId, "1");
            Assert.AreEqual(theReceivedUpdate.Data[1].FourierLength, 64);
            Assert.AreEqual(theReceivedUpdate.Data[1].SamplingRate, 250);
            Assert.AreEqual(theReceivedUpdate.Data[1].Units, string.Empty);
        }

        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateBandPowerDataStream_ConnectedDisableStream_EndsStream()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);
            var stream = aDeviceManagerClient.BandPowerStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.BandPowerDataQueue == null && count < 10)
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
            aDeviceManagerClient.BandPowerStream(new SetDataStreamEnable() { EnableStream = false, Name = "//summit/bridge/foo/device/bar" });

            // Read the event out of the client stream and check values
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            bool streamStatusCode = stream.GetStatus().StatusCode == StatusCode.OK;

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that the closed stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext);
            Assert.IsTrue(streamStatusCode);
        }
        #endregion Power Domain Streaming Tests

        #region Inertial Streaming Tests
        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateInertialDataStream_NoCachedConnection_ReturnsEmpty()
        {
            // First, create a summit service
            var mockManager = new Mock<ISummitManager>();
            var repository = Repository.GetRepositoryInstance();
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connects to the service.
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);

            // Attempt to connect to the stream when there is no device connected
            var stream = aDeviceManagerClient.InertialStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo" });

            // Save test values for later asserts
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            bool streamStatusCode = stream.GetStatus().StatusCode == StatusCode.OK;

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that the returned stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext, "Move Next returned true, expected false");
            Assert.IsTrue(streamStatusCode, "Get status was not 'OK'");
        }

        // Tests Requirement 5.2.9 - Data Streaming
        // Tests Requirement 6.1.2 - Only stream to one client
        [Test]
        public async Task CreateInertialDataStream_AlreadyConnected_ReturnsEmpty()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientA = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelA);
            var streamA = aDeviceManagerClientA.InertialStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.InertialDataQueue == null && count < 10)
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientB = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelB);
            var streamB = aDeviceManagerClientB.InertialStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });

            // Save test values for later asserts
            bool streamMoveNext = await streamB.ResponseStream.MoveNext();
            bool streamStatusCode = streamB.GetStatus().StatusCode == StatusCode.OK;

            // Failed to connect, leave test rig in correct form by closing out the server
            aTestServer.StopServer();

            // Expected behavior is that the returned stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext, "Move Next returned true, expected false");
            Assert.IsTrue(streamStatusCode, "Get status was not 'OK'");
        }

        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateInertialDataStream_EnableStreamRequestData_ReturnsStream()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);
            var stream = aDeviceManagerClient.InertialStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.InertialDataQueue == null && count < 10)
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

            // Create the fake event data
            List<short> channelXdata = new List<short>() { 0, 1, 2, 3, 4 };
            List<short> channelYdata = new List<short>() { 5, 6, 7, 8, 9 };
            List<short> channelZdata = new List<short>() { 10, 11, 12, 13, 14 };

            AccelPacket aNewAccelPacket = new AccelPacket();
            aNewAccelPacket.Header.DataTypeSequence = 1;
            aNewAccelPacket.Header.GlobalSequence = 2;
            aNewAccelPacket.Header.SystemTick = 3;
            aNewAccelPacket.Header.Timestamp = new Medtronic.NeuroStim.Olympus.DataTypes.Core.TimeOfDay(4);
            aNewAccelPacket.XSamples = channelXdata;
            aNewAccelPacket.YSamples = channelYdata;
            aNewAccelPacket.ZSamples = channelZdata;
            aNewAccelPacket.SampleRate = AccelSampleRate.Sample08;

            DateTime genTimeTestValue = DateTime.Now.Subtract(new TimeSpan(0, 0, 1));
            DateTime rxTimeTestValue = DateTime.Now;
            SensingEventAccel aNewInertialEvent = new SensingEventAccel(new double[] { 1, 1, 1 }, aNewAccelPacket, genTimeTestValue, rxTimeTestValue);

            // Use the mock framework to raise a time domain event in the 
            mockDevice.Raise(m => m.DataReceivedAccelHandler += null, aNewInertialEvent);

            // Read the event out of the client stream and check values
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            InertialUpdate theReceivedUpdate = stream.ResponseStream.Current;

            // Close out the server
            aTestServer.StopServer();

            // Check asserts for expected values
            Assert.IsTrue(streamMoveNext, "Move Next returned false, expected true");
            Assert.AreEqual(theReceivedUpdate.Name, "//summit/bridge/foo/device/bar");
            Assert.AreEqual(theReceivedUpdate.Header.DataTypeSequenceNumber, 1);
            Assert.AreEqual(theReceivedUpdate.Header.GlobalSequenceNumber, 2);
            Assert.AreEqual(theReceivedUpdate.Header.InsTick, 3);
            Assert.AreEqual(theReceivedUpdate.Header.InsTimestamp, 4);
            Assert.AreEqual(theReceivedUpdate.Header.SystemEstDeviceTxTime, genTimeTestValue.ToFileTimeUtc());
            Assert.AreEqual(theReceivedUpdate.Header.SystemRxTime, rxTimeTestValue.ToFileTimeUtc());

            Assert.AreEqual(theReceivedUpdate.Data[0].ChannelId, "Accelerometer");
            Assert.AreEqual(theReceivedUpdate.Data[0].ChannelAxis, imu_axis.X);
            Assert.AreEqual(theReceivedUpdate.Data[0].ChannelData, channelXdata);
            Assert.AreEqual(theReceivedUpdate.Data[0].SamplingRate, 8);
            Assert.AreEqual(theReceivedUpdate.Data[0].Units, aNewInertialEvent.Units);

            Assert.AreEqual(theReceivedUpdate.Data[1].ChannelId, "Accelerometer");
            Assert.AreEqual(theReceivedUpdate.Data[1].ChannelAxis, imu_axis.Y);
            Assert.AreEqual(theReceivedUpdate.Data[1].ChannelData, channelYdata);
            Assert.AreEqual(theReceivedUpdate.Data[1].SamplingRate, 8);
            Assert.AreEqual(theReceivedUpdate.Data[1].Units, aNewInertialEvent.Units);

            Assert.AreEqual(theReceivedUpdate.Data[2].ChannelId, "Accelerometer");
            Assert.AreEqual(theReceivedUpdate.Data[2].ChannelAxis, imu_axis.Z);
            Assert.AreEqual(theReceivedUpdate.Data[2].ChannelData, channelZdata);
            Assert.AreEqual(theReceivedUpdate.Data[2].SamplingRate, 8);
            Assert.AreEqual(theReceivedUpdate.Data[2].Units, aNewInertialEvent.Units);
        }

        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateInertialDataStream_ConnectedDisableStream_EndsStream()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);
            var stream = aDeviceManagerClient.InertialStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.InertialDataQueue == null && count < 10)
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
            aDeviceManagerClient.InertialStream(new SetDataStreamEnable() { EnableStream = false, Name = "//summit/bridge/foo/device/bar" });

            // Read the event out of the client stream and check values
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            bool streamStatusCode = stream.GetStatus().StatusCode == StatusCode.OK;

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that the closed stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext);
            Assert.IsTrue(streamStatusCode);
        }
        #endregion Inertial Streaming Tests

        #region Adaptive Streaming Tests
        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateAdaptiveDataStream_NoCachedConnection_ReturnsEmpty()
        {
            // First, create a summit service
            var mockManager = new Mock<ISummitManager>();
            var repository = Repository.GetRepositoryInstance();
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connects to the service.
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);

            // Attempt to connect to the stream when there is no device connected
            var stream = aDeviceManagerClient.AdaptiveStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo" });

            // Save test values for later asserts
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            bool streamStatusCode = stream.GetStatus().StatusCode == StatusCode.OK;

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that the returned stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext, "Move Next returned true, expected false");
            Assert.IsTrue(streamStatusCode, "Get status was not 'OK'");
        }

        // Tests Requirement 5.2.9 - Data Streaming
        // Tests Requirement 6.1.2 - Only stream to one client
        [Test]
        public async Task CreateAdaptiveDataStream_AlreadyConnected_ReturnsEmpty()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientA = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelA);
            var streamA = aDeviceManagerClientA.AdaptiveStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.AdaptiveDataQueue == null && count < 10)
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientB = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelB);
            var streamB = aDeviceManagerClientB.AdaptiveStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });

            // Save test values for later asserts
            bool streamMoveNext = await streamB.ResponseStream.MoveNext();
            bool streamStatusCode = streamB.GetStatus().StatusCode == StatusCode.OK;

            // Failed to connect, leave test rig in correct form by closing out the server
            aTestServer.StopServer();

            // Expected behavior is that the returned stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext, "Move Next returned true, expected false");
            Assert.IsTrue(streamStatusCode, "Get status was not 'OK'");
        }

        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateAdaptiveDataStream_EnableStreamRequestData_ReturnsStream()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);
            var stream = aDeviceManagerClient.AdaptiveStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.AdaptiveDataQueue == null && count < 10)
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

            // Create the fake event data
            DetectionAdaptivePacket aNewDetectionPacket = new DetectionAdaptivePacket();
            aNewDetectionPacket.Header.DataTypeSequence = 1;
            aNewDetectionPacket.Header.GlobalSequence = 2;
            aNewDetectionPacket.Header.SystemTick = 3;
            aNewDetectionPacket.Header.Timestamp = new Medtronic.NeuroStim.Olympus.DataTypes.Core.TimeOfDay(4);
            aNewDetectionPacket.CurrentAdaptiveState = 1;
            aNewDetectionPacket.CurrentProgramAmplitudes = new byte[] { 2, 3, 4, 5 };
            aNewDetectionPacket.IsInHoldOffOnStartup = false;
            aNewDetectionPacket.Ld0DetectionStatus = DetectionOutputStatus.HighDetect;
            aNewDetectionPacket.Ld0Status = new LinearDiscriminantStatus() { FeatureInputs = new uint[] { 1, 2, 3, 4, 5, 6, 7, 8 }, FixedDecimalPoint = 0xFF, HighThreshold = 16, LowThreshold = 10, Output = 8 };
            aNewDetectionPacket.Ld1DetectionStatus = DetectionOutputStatus.LowImmediateDetect;
            aNewDetectionPacket.Ld1Status = new LinearDiscriminantStatus() { FeatureInputs = new uint[] { 9, 10, 11, 12, 13, 14, 15, 16 }, FixedDecimalPoint = 0xEE, HighThreshold = 2, LowThreshold = 12, Output = 9 };
            aNewDetectionPacket.PreviousAdaptiveState = 9;
            aNewDetectionPacket.SensingStatus = SenseStates.AdaptiveStim | SenseStates.DetectionLd0;
            aNewDetectionPacket.StateEntryCount = 2;
            aNewDetectionPacket.StateTime = 1024;
            aNewDetectionPacket.StimFlags = AdaptiveTherapyStatusFlags.Prog0AmpRamping | AdaptiveTherapyStatusFlags.Prog2AmpRamping;
            aNewDetectionPacket.StimRatePeriod = 256;

            DateTime genTimeTestValue = DateTime.Now.Subtract(new TimeSpan(0, 0, 1));
            DateTime rxTimeTestValue = DateTime.Now;
            AdaptiveDetectEvent aNewDetectorEvent = new AdaptiveDetectEvent(aNewDetectionPacket, genTimeTestValue, rxTimeTestValue);

            // Use the mock framework to raise a time domain event in the 
            mockDevice.Raise(m => m.DataReceivedDetectorHandler += null, aNewDetectorEvent);

            // Read the event out of the client stream and check values
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            AdaptiveUpdate theReceivedUpdate = stream.ResponseStream.Current;

            // Close out the server
            aTestServer.StopServer();

            // Check asserts for expected values
            Assert.IsTrue(streamMoveNext, "Move Next returned false, expected true");
            Assert.AreEqual(theReceivedUpdate.Name, "//summit/bridge/foo/device/bar");
            Assert.AreEqual(theReceivedUpdate.Header.DataTypeSequenceNumber, 1);
            Assert.AreEqual(theReceivedUpdate.Header.GlobalSequenceNumber, 2);
            Assert.AreEqual(theReceivedUpdate.Header.InsTick, 3);
            Assert.AreEqual(theReceivedUpdate.Header.InsTimestamp, 4);
            Assert.AreEqual(theReceivedUpdate.Header.SystemEstDeviceTxTime, genTimeTestValue.ToFileTimeUtc());
            Assert.AreEqual(theReceivedUpdate.Header.SystemRxTime, rxTimeTestValue.ToFileTimeUtc());

            Assert.AreEqual(theReceivedUpdate.CurrentAmplitudes.ToList(), aNewDetectionPacket.CurrentProgramAmplitudesInMilliamps.ToList());
            Assert.AreEqual(theReceivedUpdate.CurrentFrequency, aNewDetectionPacket.StimRateInHz);
            Assert.AreEqual(theReceivedUpdate.CurrentState, aNewDetectionPacket.CurrentAdaptiveState);
            Assert.AreEqual(theReceivedUpdate.CurrentStateCount, aNewDetectionPacket.StateEntryCount);
            Assert.AreEqual(theReceivedUpdate.CurrentStateTotalTime, aNewDetectionPacket.StateTime);
            Assert.AreEqual(theReceivedUpdate.InHoldoff, aNewDetectionPacket.IsInHoldOffOnStartup);
            Assert.AreEqual(theReceivedUpdate.PreviousState, aNewDetectionPacket.PreviousAdaptiveState);

            Assert.AreEqual(theReceivedUpdate.DetectorStatus[0].FeatureInputs, aNewDetectionPacket.Ld0Status.FeatureInputs);
            Assert.AreEqual(theReceivedUpdate.DetectorStatus[0].FixedDecimalSetpoint, aNewDetectionPacket.Ld0Status.FixedDecimalPoint);
            Assert.AreEqual(theReceivedUpdate.DetectorStatus[0].HighThreshold, aNewDetectionPacket.Ld0Status.HighThreshold);
            Assert.AreEqual(theReceivedUpdate.DetectorStatus[0].LowThreshold, aNewDetectionPacket.Ld0Status.LowThreshold);
            Assert.AreEqual(theReceivedUpdate.DetectorStatus[0].Output, aNewDetectionPacket.Ld0Status.Output);

            Assert.AreEqual(theReceivedUpdate.DetectorStatus[1].FeatureInputs, aNewDetectionPacket.Ld1Status.FeatureInputs);
            Assert.AreEqual(theReceivedUpdate.DetectorStatus[1].FixedDecimalSetpoint, aNewDetectionPacket.Ld1Status.FixedDecimalPoint);
            Assert.AreEqual(theReceivedUpdate.DetectorStatus[1].HighThreshold, aNewDetectionPacket.Ld1Status.HighThreshold);
            Assert.AreEqual(theReceivedUpdate.DetectorStatus[1].LowThreshold, aNewDetectionPacket.Ld1Status.LowThreshold);
            Assert.AreEqual(theReceivedUpdate.DetectorStatus[1].Output, aNewDetectionPacket.Ld1Status.Output);

            // Enums require a bit of casting since types are different. Flag enums require check for contains.
            Assert.AreEqual(theReceivedUpdate.DetectorState[0], (DetectorState)aNewDetectionPacket.Ld0DetectionStatus);
            Assert.AreEqual(theReceivedUpdate.DetectorState[1], (DetectorState)aNewDetectionPacket.Ld1DetectionStatus);
            Assert.AreEqual(theReceivedUpdate.AdaptiveStimRamping.Count, 2);
            Assert.IsTrue(theReceivedUpdate.AdaptiveStimRamping.Contains(AdaptiveRampingFlags.Prog0AmpRamping));
            Assert.IsTrue(theReceivedUpdate.AdaptiveStimRamping.Contains(AdaptiveRampingFlags.Prog2AmpRamping));
            Assert.AreEqual(theReceivedUpdate.SensorsEnabled.Count, 2);
            Assert.IsTrue(theReceivedUpdate.SensorsEnabled.Contains((SensingEnables)SenseStates.AdaptiveStim));
            Assert.IsTrue(theReceivedUpdate.SensorsEnabled.Contains((SensingEnables)SenseStates.DetectionLd0));
        }

        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateAdaptiveDataStream_ConnectedDisableStream_EndsStream()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);
            var stream = aDeviceManagerClient.AdaptiveStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.AdaptiveDataQueue == null && count < 10)
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
            aDeviceManagerClient.AdaptiveStream(new SetDataStreamEnable() { EnableStream = false, Name = "//summit/bridge/foo/device/bar" });

            // Read the event out of the client stream and check values
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            bool streamStatusCode = stream.GetStatus().StatusCode == StatusCode.OK;

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that the closed stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext);
            Assert.IsTrue(streamStatusCode);
        }
        #endregion Adaptive Streaming Tests

        #region Loop Record Streaming Tests
        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateLoopDataStream_NoCachedConnection_ReturnsEmpty()
        {
            // First, create a summit service
            var mockManager = new Mock<ISummitManager>();
            var repository = Repository.GetRepositoryInstance();
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connects to the service.
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);

            // Attempt to connect to the stream when there is no device connected
            var stream = aDeviceManagerClient.LoopRecordUpdateStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo" });

            // Save test values for later asserts
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            bool streamStatusCode = stream.GetStatus().StatusCode == StatusCode.OK;

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that the returned stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext, "Move Next returned true, expected false");
            Assert.IsTrue(streamStatusCode, "Get status was not 'OK'");
        }

        // Tests Requirement 5.2.9 - Data Streaming
        // Tests Requirement 6.1.2 - Only stream to one client
        [Test]
        public async Task CreateLoopDataStream_AlreadyConnected_ReturnsEmpty()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientA = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelA);
            var streamA = aDeviceManagerClientA.LoopRecordUpdateStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.LoopRecordQueue == null && count < 10)
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientB = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelB);
            var streamB = aDeviceManagerClientB.LoopRecordUpdateStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });

            // Save test values for later asserts
            bool streamMoveNext = await streamB.ResponseStream.MoveNext();
            bool streamStatusCode = streamB.GetStatus().StatusCode == StatusCode.OK;

            // Failed to connect, leave test rig in correct form by closing out the server
            aTestServer.StopServer();

            // Expected behavior is that the returned stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext, "Move Next returned true, expected false");
            Assert.IsTrue(streamStatusCode, "Get status was not 'OK'");
        }

        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateLoopDataStream_EnableStreamRequestData_ReturnsStream()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);
            var stream = aDeviceManagerClient.LoopRecordUpdateStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.LoopRecordQueue == null && count < 10)
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

            // Create the fake event data
            Medtronic.NeuroStim.Olympus.DataTypes.Sensing.LoopRecordingTriggers LoopTrigger = Medtronic.NeuroStim.Olympus.DataTypes.Sensing.LoopRecordingTriggers.State3;
            LoopRecordingFlags LoopFlags = LoopRecordingFlags.Triggered;

            DateTime genTimeTestValue = DateTime.Now.Subtract(new TimeSpan(0, 0, 1));
            DateTime rxTimeTestValue = DateTime.Now;
            LoopRecordUpdateEvent aNewLoopEvent = new LoopRecordUpdateEvent(LoopTrigger, LoopFlags, genTimeTestValue, rxTimeTestValue);

            // Use the mock framework to raise a time domain event in the mock device
            mockDevice.Raise(m => m.DataReceivedLoopRecordUpdateHandler += null, aNewLoopEvent);

            // Read the event out of the client stream and check values
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            LoopRecordUpdate theReceivedUpdate = stream.ResponseStream.Current;

            // Close out the server
            aTestServer.StopServer();

            // Check asserts for expected values
            Assert.IsTrue(streamMoveNext, "Move Next returned false, expected true");
            Assert.AreEqual(theReceivedUpdate.Name, "//summit/bridge/foo/device/bar");
            Assert.AreEqual(theReceivedUpdate.Header.SystemRxTime, rxTimeTestValue.ToFileTimeUtc());
            Assert.AreEqual(theReceivedUpdate.Header.SystemEstDeviceTxTime, genTimeTestValue.ToFileTimeUtc());
            Assert.AreEqual(theReceivedUpdate.Triggers.Count, 1);
            Assert.IsTrue(theReceivedUpdate.Triggers.Contains((OpenMind.LoopRecordTriggers)Medtronic.NeuroStim.Olympus.DataTypes.Sensing.LoopRecordingTriggers.State3));
            Assert.AreEqual(theReceivedUpdate.Flags.Count, 1);
            Assert.IsTrue(theReceivedUpdate.Flags.Contains((OpenMind.LoopRecordFlags)LoopRecordingFlags.Triggered));
        }

        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateLoopDataStream_ConnectedDisableStream_EndsStream()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);
            var stream = aDeviceManagerClient.LoopRecordUpdateStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.LoopRecordQueue == null && count < 10)
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
            aDeviceManagerClient.LoopRecordUpdateStream(new SetDataStreamEnable() { EnableStream = false, Name = "//summit/bridge/foo/device/bar" });

            // Read the event out of the client stream and check values
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            bool streamStatusCode = stream.GetStatus().StatusCode == StatusCode.OK;

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that the closed stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext);
            Assert.IsTrue(streamStatusCode);
        }
        #endregion Loop Record Streaming Tests

        #region Echo Streaming Tests
        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateEchoDataStream_NoCachedConnection_ReturnsEmpty()
        {
            // First, create a summit service
            var mockManager = new Mock<ISummitManager>();
            var repository = Repository.GetRepositoryInstance();
            var summitService = new SummitService(repository, mockManager.Object);
            OmniServer aTestServer = new OmniServer(repository, summitService);
            aTestServer.StartServer();

            // Create a client that connects to the service.
            Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);

            // Attempt to connect to the stream when there is no device connected
            var stream = aDeviceManagerClient.EchoStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo" });

            // Save test values for later asserts
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            bool streamStatusCode = stream.GetStatus().StatusCode == StatusCode.OK;

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that the returned stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext, "Move Next returned true, expected false");
            Assert.IsTrue(streamStatusCode, "Get status was not 'OK'");
        }

        // Tests Requirement 5.2.9 - Data Streaming
        // Tests Requirement 6.1.2 - Only stream to one client
        [Test]
        public async Task CreateEchoDataStream_AlreadyConnected_ReturnsEmpty()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientA = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelA);
            var streamA = aDeviceManagerClientA.EchoStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.EchoDataQueue == null && count < 10)
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClientB = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channelB);
            var streamB = aDeviceManagerClientB.EchoStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });

            // Save test values for later asserts
            bool streamMoveNext = await streamB.ResponseStream.MoveNext();
            bool streamStatusCode = streamB.GetStatus().StatusCode == StatusCode.OK;

            // Failed to connect, leave test rig in correct form by closing out the server
            aTestServer.StopServer();

            // Expected behavior is that the returned stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext, "Move Next returned true, expected false");
            Assert.IsTrue(streamStatusCode, "Get status was not 'OK'");
        }

        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateEchoDataStream_EnableStreamRequestData_ReturnsStream()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);
            var stream = aDeviceManagerClient.EchoStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.EchoDataQueue == null && count < 10)
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

            // Create the fake event data
            byte testByte = 0xAA;
            DateTime genTimeTestValue = DateTime.Now.Subtract(new TimeSpan(0, 0, 1));
            DateTime rxTimeTestValue = DateTime.Now;
            ExternalMarkerEchoEvent aNewEchoEvent = new ExternalMarkerEchoEvent(testByte, genTimeTestValue, rxTimeTestValue);

            // Use the mock framework to raise a time domain event in the mock device
            mockDevice.Raise(m => m.DataReceivedMarkerEchoHandler += null, aNewEchoEvent);

            // Read the event out of the client stream and check values
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            EchoUpdate theReceivedUpdate = stream.ResponseStream.Current;

            // Close out the server
            aTestServer.StopServer();

            // Check asserts for expected values
            Assert.IsTrue(streamMoveNext, "Move Next returned false, expected true");
            Assert.AreEqual(theReceivedUpdate.Name, "//summit/bridge/foo/device/bar");
            Assert.AreEqual(theReceivedUpdate.Header.SystemRxTime, rxTimeTestValue.ToFileTimeUtc());
            Assert.AreEqual(theReceivedUpdate.Header.SystemEstDeviceTxTime, genTimeTestValue.ToFileTimeUtc());
            Assert.AreEqual(theReceivedUpdate.EchoByte, testByte);
        }

        // Tests Requirement 5.2.9 - Data Streaming
        [Test]
        public async Task CreateEchoDataStream_ConnectedDisableStream_EndsStream()
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
            OpenMind.DeviceManagerService.DeviceManagerServiceClient aDeviceManagerClient = new OpenMind.DeviceManagerService.DeviceManagerServiceClient(channel);
            var stream = aDeviceManagerClient.EchoStream(new SetDataStreamEnable() { EnableStream = true, Name = "//summit/bridge/foo/device/bar" });
            SummitServiceInfo theCachedConnection = repository.GetConnectionByName("//summit/bridge/foo");
            int count = 0;
            while (theCachedConnection.EchoDataQueue == null && count < 10)
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
            aDeviceManagerClient.EchoStream(new SetDataStreamEnable() { EnableStream = false, Name = "//summit/bridge/foo/device/bar" });

            // Read the event out of the client stream and check values
            bool streamMoveNext = await stream.ResponseStream.MoveNext();
            bool streamStatusCode = stream.GetStatus().StatusCode == StatusCode.OK;

            // Close out the server
            aTestServer.StopServer();

            // Expected behavior is that the closed stream returns false, indicating a completed (empty) stream
            Assert.IsFalse(streamMoveNext);
            Assert.IsTrue(streamStatusCode);
        }
        #endregion Echo Streaming Tests
    }
}

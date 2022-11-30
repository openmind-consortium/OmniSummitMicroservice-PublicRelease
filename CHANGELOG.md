# [1.11.0](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.10.2...v1.11.0) (2022-11-30)


### Bug Fixes

* adding software test version numbers to stream enable and disable tests. adding mockManager.verifyAll() to verify the test functions were called ([a7a8bb1](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/a7a8bb15d6560173a02fc8505ad9e4be9c9204e2))
* changing failure test names from StreamEnable_ValidRequestParameter_ReturnsFailure to StreamEnable_ErrorEncountered_ReturnsFailure (and same for disable test) ([b4c6a0b](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/b4c6a0b187c27d6f0acb17a384db0bbf0aeec78c))
* StreamEnable and StreamDisable Mock Setup ([b7a299e](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/b7a299eb4cb47ec162f6469256e4a151616327fd))


### Features

* adding tests for StreamEnable and StreamDisable ([9c1e755](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/9c1e7552b7681ff997e28ce836525697ddefa1cb))
* finished all stream enable and stream disable tests ([f9bad18](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/f9bad18491d5a3fcfba0858a377d1a0eef58c9eb))
* finished StreamEnable_InvalidRequestParameter_ReturnsUnknown and StreamDisable_InvalidRequestParameter_ReturnsUnknown ([ab4a289](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/ab4a28975167aec710002f52e2c86fa9129e707a))
* finished StreamEnable_ValidRequestParameter_ReturnsSuccess ([bf5e755](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/bf5e7555fcfec8ed70f9adc9dbc911b5cea1bbb5))
* implemented StreamDisable_ValidRequestParameter_ReturnsSuccess ([52c3adb](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/52c3adb593da993d4104b0f43d4234a3aa20ada6))

## [1.10.2](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.10.1...v1.10.2) (2022-11-17)


### Bug Fixes

* line error ([b45dbd1](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/b45dbd1bd181449a3966e55401935f2ebad43360))
* powerbands test ([f6d7617](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/f6d7617e603cd6cfc9fdea24dee103ae92968dd1))
* sensing state value ([f5078cc](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/f5078cc92e52a5c3eaa9011509f8e2f7ca869e86))

## [1.10.1](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.10.0...v1.10.1) (2022-10-27)


### Bug Fixes

*  add SystemEstDeviceTxTime to all data steams ([f1d818c](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/f1d818c9c7f8bd8152f29206fd014d4de32f9c27))
* packet gen time crash in time domain data. ([836f535](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/836f53539f9769d88d46151e3d0a0fa492eded30))

# [1.10.0](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.9.1...v1.10.0) (2022-10-06)


### Features

* OmniSummitDeviceServiceSetup project for creating installer ([e6c5ec4](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/e6c5ec4d5c0a24a2af337276fcaff668815ede9d))

## [1.9.1](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.9.0...v1.9.1) (2022-09-16)


### Bug Fixes

* multiple connection tests also close server before asserts. ([6b21345](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/6b21345d553bc72ef69dea06ea0176148605e2b2))
* remainder of streaming tests stop server before calling an assert. ([f37aaf3](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/f37aaf341138ec7c64ce3d7cd41940f187b1a540))
* stream connection status now closes server before asserts ([a6ce973](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/a6ce973c3175aad93fee89a5f9f39d4865bc2eca))
* update to time domain tests to stop server before asserts ([621d1b3](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/621d1b36815320f031dc17b56af5cecd40cb3537))

# [1.9.0](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.8.0...v1.9.0) (2022-09-15)


### Bug Fixes

* cannot declare a 'var' variable null ([94132f7](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/94132f76c9127af6055577be53bb1b086a478ee7))
* changed libraries folder name to Libraries ([bf05843](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/bf05843ee4b7a06bdf5e43076cc522d5a4016ba6))
* changed Libraries folder to reflect csproj hintpaths ([3ead384](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/3ead384ff88355d5da0634fc98ac11658ca95c3e))
* changed libraries name ([8439774](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/8439774372000a961b5f22c2bfe2aafb36355b10))
* fix ConnectToBridge calls ([cdc12a9](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/cdc12a9c5cd0383dc3cb68f957b61312b3f5c04c))
* fixed Startup_CreatesWindow bug ([d80a024](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/d80a024c93652b245578fb5aa487f310d0e6100d))
* food typos ([539b1f8](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/539b1f8115b99c34d785c4123b96871bfc3b1ac8))
* instrumentInfo caching ([a871764](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/a8717641ee56fd0f7269dd5bbda8d963aaa57029))
* remoe old test ([71158a4](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/71158a46334af8111189ac0860328d4710232f96))
* stop servers before asserts ([783aa5c](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/783aa5c48e40604bad95680e7452209c4bde9f46))
* typo in summitService ([aa87a36](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/aa87a36886b0faf066849150ecac8d32d0045bbc))
* update ConnectToDevice_RequestedDeviceNotCached_ReturnsError test ([0bd6f83](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/0bd6f839f4530153e43912ec13f70fdb2dba0b42))
* updated csproj hintpaths for summit DLLs ([35b5dd4](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/35b5dd42b28a494cf2497091361f2cfc80d7a4f7))


### Features

* add ConnectToBridge_SummitInRepoNotDisposed_ReturnsSuccess ([b5dba51](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/b5dba51d60111e37081ba78542fe06d8f8532c1d))
* add signatures for ConnectToBridge tests ([553a8a7](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/553a8a75c0a5a99acb390a6df29cbd999bc0196c))
* ConnectToBridge UnitTests ([89bb82b](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/89bb82b14df33fe01ef2288766f4bc6520fc8a89))
* ConnectToBridge_SummitInRepoIsDisposed_ReturnsSuccess ([79f014a](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/79f014a8a7ffe78bb6e66ad22404f77f2999529a))
* ConnectToBridge_SummitNotInRepo_ReturnsSuccess ([549cd03](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/549cd03118ce48c14c1e1c834a6f36f42dcd8f0a))
* ConnectToDevice test ([c4ceac8](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/c4ceac877a46f7c471768482985b22c7ec5fda57))
* ConnectToDevice_CachedConnectioninRepoIsDisposed_ReturnsSuccess ([7b5c3b6](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/7b5c3b634a6a6a3379ce98f93691ad99551899af))
* ConnectToDevice_CachedConnectionInRepoNotDisposed_ReturnsSuccess ([6f5ff57](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/6f5ff5707300b342472062b9d2cf4df9d2ea4f2b))
* null connects require the request name to be just the bridge name ([cbdd40d](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/cbdd40d7ee79802b861ad579fcf0609521806af9))

# [1.8.0](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.7.0...v1.8.0) (2022-09-09)


### Bug Fixes

* adaptive streaming test now checks that only two flags are in the adaptive stim ramping and sensors enabled enums ([5e1f133](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/5e1f13361c29c43da252a17ed967d05a83c43f7d))
* echo and loop stream can deal with null headers from Summit API ([92cc355](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/92cc3557334cbbf03ecc32c2555408d1596b203f))
* streams that return flag-enums now do not always respond with default value 0 (usually 'none' or equivalent) ([8fc6134](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/8fc6134a8c4bb983365415166276573259034b63))


### Features

* new tests for start echo and loop stream ([710d3e1](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/710d3e123453ddd95472aac93b271c0eb2806e68))

# [1.7.0](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.6.0...v1.7.0) (2022-08-11)


### Bug Fixes

* adding a delay to ensure window closes ([04fe1ac](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/04fe1ac2766b754b79461c0a80ca126ac8291e0a))
* DescribeBridge now handles edge case where returned telemetry info object has a null string ([57c0fea](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/57c0fea3f77124c4acef72ea5b5621520f5c0ad3))
* new tests now close server after executing ([79c0f67](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/79c0f678114b220ef2f2828bb28f57987620c65d))
* service functions do not check for null bridge/device repository returns ([d21cb01](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/d21cb0185bd13bb2f14f273837f503d6098adc24))
* Startup_CreatesWindow_qToShutdown failing ([4329ddd](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/4329ddd3d5f293141544430fcaf01b24aaff92c7))
* Startup_PrintsToConsole now kills process. Startup_CreatesWindow_qToShutdown now appropriately creates process for redirection of standard input ([0a4061a](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/0a4061aa607837b6ae020041b9df9c6b11ff5d20))


### Features

* DeviceStatus_DeviceDoesNotExist_ReturnsError test implemented ([703e25d](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/703e25dbcbfd16af12ada5995a2e5fbe4975d531))
* new test DescribeBridge_NoErrorFromAPI_ReturnsDetails ([2b1fe53](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/2b1fe5331d54ffdd38e4b0bac48657e8c1e790d0))
* new test for server shutdown by 'q' ([1e9e5c9](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/1e9e5c9f672ab32f5dcc16cb9d1fb5b6224578a8))

# [1.6.0](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.5.0...v1.6.0) (2022-08-04)


### Features

* new test ConfigureBeep_ErrorEncountered_ReturnsError ([79d218f](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/79d218f4051511461aacbe3b1bb4ece372a5bf86))

# [1.5.0](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.4.0...v1.5.0) (2022-08-02)


### Features

* implement ConfigureBeep_InvalidRequest_ReturnsError ([1edc9f4](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/1edc9f48ac242375e230b8117459df4f11efe64f))

# [1.4.0](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.3.1...v1.4.0) (2022-07-26)


### Bug Fixes

* adding an assert that windowhandle not null in Startup_CreatesWindow ([cdd83b8](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/cdd83b8c9cfed92151cedbe0e9c0fefc7ae9dfcf))
* all tests now connect to gRPC microservice ([d37a17f](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/d37a17f22a15458e76bf521792556a885cc64a37))
* checking window not null prior to closing ([3bfd690](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/3bfd6909bae8165ea723f6a43a04dcb5df9747e6))
* killing process at end of printstoconsole ([a3fc511](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/a3fc51130d3e2a9f6d27b5a3b7bac599893474e7))
* potential fix for github not successfully executing Startup_PrintsToConsole ([1eb325a](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/1eb325af6fe42880bf8c53ba34ea36243f9a454f))
* reorganizing process kill location for test robustness on github actions ([95f33f4](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/95f33f4091ae781d7fd73c812975ac427c4beff6))
* Startup_CreatesWindow not closing window on failure ([1732be8](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/1732be8512ecec532626f048f66303d38ae70e8c))
* tests not utilizing gRPC channel ([9365bc4](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/9365bc472ced4df921bac2f4f29c186c6ff04411))


### Features

* adding new test function handles for missing testing functionality ([3ce295d](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/3ce295d0f551621f29dfcecc8c2d2e19daa6860f))

## [1.3.1](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.3.0...v1.3.1) (2022-05-18)


### Bug Fixes

* assert fail strings updated with proper client ([68135d3](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/68135d3164a107abc39c384e789bb330e2230376))
* correct TwoClientsTwoDevices test channel B device status call ([d01bf31](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/d01bf31650ed4387157be261e2161e5fc8eedf7a))
* more assert fail strings updated with proper client/typos ([d1820d9](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/d1820d97793a266d9258b938134e1daeb93e75d6))

# [1.3.0](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.2.0...v1.3.0) (2022-02-24)


### Features

* Connection Monitor now reports source of reconnection failures (INS or CTM) ([2140d4b](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/2140d4bee4b97d53059e665f50f52102358165e3))
* start of tests for reconnect failure/success behavior ([2a55386](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/2a55386c72eea2d4004082970762cafafa6463cb))
* tests for connection monitoring reconnection logic ([2024c4f](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/2024c4f03a339dbaa7767e27d37dd7c4133c0123))

# [1.2.0](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.1.1...v1.2.0) (2022-02-10)


### Bug Fixes

* ConnectionMonitoring_DisconnectionEventReceivedSummitDisposed_Notify no longer hangs ([26588cf](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/26588cf9fe2f843d12a503dfa432496877101ca2))
* mock setup sequence corrections. ([feed55c](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/feed55cab45eb8ec9651ba0ab216ef677aedd7ad))
* need to check status message as a value not flag ([e73c3cd](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/e73c3cdd2437351f6abb056951f961c14b499eb4))
* Repository.RepositoryItem_UnexpectedDisposalHandler should remove object from repo by bridge-name instead of by bridge-serial ([af43c34](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/af43c3445fcf7c1a5e49a5c2851a22a19158cea2))
* streaming functions need to be 'async' for proper response stream functionality ([2d2fdd3](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/2d2fdd3842cecaa5c24e3ea90f03b235a894a7a6))
* TheSummitSyste_UnexpectedCtmDisconnectHandler should use 'this.Summit' instead of event sender parameter ([6dac64b](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/6dac64bc1f0b4eaf9988a7a56ae0b9242046aa43))
* threaded events for raising disconnect handlers causing test hangs. ([811818b](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/811818b93e7c7668d4aaaabcb1c5931e99397cbb))
* update disposal info object flag before closing everything else out. ([01307f2](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/01307f26116ab1ed43834e2805d81010ee98ec4c))


### Features

* Adding basic Connection Monitoring Stream Tests ([320a077](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/320a077cf43fad324246bacb716f21842b2a4abf))
* adding initial structure of basic reconnection tests ([e90f821](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/e90f82141aa87dda39b8dc7643e823eada64fafa))

## [1.1.1](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.1.0...v1.1.1) (2022-02-09)


### Bug Fixes

* memory leak and unconnectable INS in reconnect helper function ([53d034a](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/53d034ab9fc51777e6e268a9176c75980086c3de))

# [1.1.0](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.0.4...v1.1.0) (2022-01-14)


### Bug Fixes

* casting to summit beep config ([06c2b12](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/06c2b1259b4c97e9df01a5e90de5f413b0533857))
* change request name ([d15f043](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/d15f0434b885e486c4d85a1d652c7139aedc4093))
* configure beep test ([46fab30](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/46fab309ecd6022abeefb961dfe52c3d25c42839))
* fix variable ([6b56bc9](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/6b56bc95b7168fdb1d41dbb8aa774a8ef92c5729))
* try true case ([043dd7b](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/043dd7bd0e0813157625e7ef10667362872e8cfb))


### Features

* test for configure beep ([be3ed34](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/be3ed34bb5301ba453d4b03881beaac9f1012ee3))

## [1.0.4](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.0.3...v1.0.4) (2022-01-12)


### Bug Fixes

* tests for requirements 4.3, 5.1.1, 5.1.3 ([205e132](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/205e132ff16d4930e5e8e5d8d96e86c4a7f33cab))

## [1.0.3](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.0.2...v1.0.3) (2021-12-01)


### Bug Fixes

* fix syntax error in release workflow ([445fb33](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/445fb338e3339e1e172c543d006646926dc2459f))

## [1.0.2](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.0.1...v1.0.2) (2021-12-01)


### Bug Fixes

* remove non-existant directory from bundle ([ae09345](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/ae093452b0200bf9d3ed7e4471f50f1d9e0a0fc6))

## [1.0.1](https://github.com/openmind-consortium/OmniSummitDeviceService/compare/v1.0.0...v1.0.1) (2021-12-01)


### Bug Fixes

* add . to make a new release ([5b65c1d](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/5b65c1d40b143eb12634f32495cb42e56e349125))

# 1.0.0 (2021-12-01)


### Bug Fixes

* add to grpc object ([db01e5e](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/db01e5e6085e78246396bbe56c557cae0fd9b60e))
* added Version property (with value 1.0.0) to csproj ([cf97d87](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/cf97d87db2bc4083595fa365322c5f05e146950b))
* bad defaults for physical layer ([865f554](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/865f554c0a41d7b0266eca852a077c223d2a77bf))
* bump protos ([0293c28](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/0293c28e05a8c391e25486f72956b967545af38a))
* connection status queue uses current number of retries instead of total. ([6d2606d](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/6d2606d26c84776a4dabe10942e593a304817797))
* delete telemetryService ([48ea771](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/48ea771ec83bc2fed6d2996874a48379ecc14a4d))
* disable time domain sense ([d6bf088](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/d6bf0885fec7f25998bb200dd1269544d2a77a32))
* fix c# array ([38f3ddd](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/38f3dddcf44f07ea3bbfdbed734659c68d2bf4ef))
* fix naming ([40a7200](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/40a72004bc567a2073f984dee6800f27e6adf518))
* fixing incorrect pathname for including the omniprotos ([342cd30](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/342cd3026977a0d4b7404308768106e821bbb58c))
* from all interfaces to localhost ([4a04399](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/4a043994f568174eafe49f18769bc4c65dbe366d))
* instantiate telemetry and fix return object ([c1ec4f0](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/c1ec4f0996d8465a7462e8c1f3c1b4a890cf59cd))
* missing semicolon ([d602128](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/d6021284d7beb7508f9c02422200897ac4878015))
* pass number of retries to reconnection logic instead of decrementing object variable directly ([5c76c7e](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/5c76c7e3240d3145576ed1e9eb06350b582c5f49))
* pull from infoservice ([9170a30](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/9170a30e1f5620329ed3e848fdd95905a93ca4b3))
* remove extra semicolon ([fb7f55d](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/fb7f55d153344d845fa670c9d67d459e5bfea059))
* removed unnecessary text from the return string for Version Number. ([d2723cb](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/d2723cbaf98e5ba48cc6e9f96834a75780a07d35))
* semicolon ([3a97872](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/3a97872151dee36f1c522cb0efecd2a365470cf1))
* Sensing Configuration Tests not properly creating 8 power band configuration objects. ([f527ba0](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/f527ba0175963dccde54da226d83987e043bbdc4))
* Set retries to 0 as default if no retries given from client. Added return from while loop if ctm disposed and retries limit was reached ([fd98578](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/fd98578f5ac2c95c2a900bdeac003d2638967dad))
* stop removing unexpectedCTMDisconnectHandler on already disposed Summit ([a2d4c4b](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/a2d4c4b7c6dd8a17da3c23328b17ab3aa647a066))
* Summit Service not assigning ratio field ([6b17987](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/6b17987635ff95e61c708004f77e94a0eebe5d7b))
* summitserviceinfo object disposes itself after failed reconnect. ([fd80a0b](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/fd80a0b3415aee55504e1c9eea909ff3d82f5548))
* test update due to new object response as opposed to past empty() ([21b30ef](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/21b30ef08631ca542681e51e2a5d1fd4d78c7bca))
* update generated code to match protos ([7bf65a1](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/7bf65a14a0eeff9ab13f052148c22ba3fac8f51d))
* update protobuf locations ([bae292d](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/bae292d9c6c53072afa8e41b98bbcec885685cfe))
* update protos ([e211ef9](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/e211ef98778f8fe456eb3ea18cbd8588b4ff84eb))
* update protos version ([3b70ffc](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/3b70ffcfb938d6d2c1e12896b9452a11d2450eb2))


### Features

* add supported devices endpoint ([99f2573](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/99f2573584269dc8f173f465175afa1dc9cea614))
* Added enable and disable streams ([91cdab9](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/91cdab96b7c62fb7404ad25b248867cfe86e8173))
* Added sense enables to configure sense ([f231217](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/f231217882ebcf84332140b6bdddd6a979dfd65b))
* added VersionNumber functionality to InfoService.cs. It reads the assembly version automatically from the project assemblies. tested using a python script ([2acd25b](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/2acd25b158da01d69105e79f436c619ec5164f18))
* Automatic reconnection failures dispose SummitServiceInfo object and remove from Repository ([456a2a8](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/456a2a89413d73c810004ae6e0e67e6edf26b7b5))
* configurable host ([2b605b4](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/2b605b42b5a692e023ab72952cbe98bc01701fd3))
* impedance tests ([92784cc](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/92784cccd66d2afc3d6bafa152d869473aebf162))
* update protos ([cde9c63](https://github.com/openmind-consortium/OmniSummitDeviceService/commit/cde9c6355411354a4b56d33c8f42671c5db73b88))

# 1.0.0 (2021-12-01)


### Bug Fixes

* add to grpc object ([db01e5e](https://github.com/openmind-consortium/OpenMindServer/commit/db01e5e6085e78246396bbe56c557cae0fd9b60e))
* added Version property (with value 1.0.0) to csproj ([cf97d87](https://github.com/openmind-consortium/OpenMindServer/commit/cf97d87db2bc4083595fa365322c5f05e146950b))
* bad defaults for physical layer ([865f554](https://github.com/openmind-consortium/OpenMindServer/commit/865f554c0a41d7b0266eca852a077c223d2a77bf))
* bump protos ([0293c28](https://github.com/openmind-consortium/OpenMindServer/commit/0293c28e05a8c391e25486f72956b967545af38a))
* connection status queue uses current number of retries instead of total. ([6d2606d](https://github.com/openmind-consortium/OpenMindServer/commit/6d2606d26c84776a4dabe10942e593a304817797))
* delete telemetryService ([48ea771](https://github.com/openmind-consortium/OpenMindServer/commit/48ea771ec83bc2fed6d2996874a48379ecc14a4d))
* disable time domain sense ([d6bf088](https://github.com/openmind-consortium/OpenMindServer/commit/d6bf0885fec7f25998bb200dd1269544d2a77a32))
* fix c# array ([38f3ddd](https://github.com/openmind-consortium/OpenMindServer/commit/38f3dddcf44f07ea3bbfdbed734659c68d2bf4ef))
* fix naming ([40a7200](https://github.com/openmind-consortium/OpenMindServer/commit/40a72004bc567a2073f984dee6800f27e6adf518))
* fixing incorrect pathname for including the omniprotos ([342cd30](https://github.com/openmind-consortium/OpenMindServer/commit/342cd3026977a0d4b7404308768106e821bbb58c))
* from all interfaces to localhost ([4a04399](https://github.com/openmind-consortium/OpenMindServer/commit/4a043994f568174eafe49f18769bc4c65dbe366d))
* instantiate telemetry and fix return object ([c1ec4f0](https://github.com/openmind-consortium/OpenMindServer/commit/c1ec4f0996d8465a7462e8c1f3c1b4a890cf59cd))
* missing semicolon ([d602128](https://github.com/openmind-consortium/OpenMindServer/commit/d6021284d7beb7508f9c02422200897ac4878015))
* pass number of retries to reconnection logic instead of decrementing object variable directly ([5c76c7e](https://github.com/openmind-consortium/OpenMindServer/commit/5c76c7e3240d3145576ed1e9eb06350b582c5f49))
* pull from infoservice ([9170a30](https://github.com/openmind-consortium/OpenMindServer/commit/9170a30e1f5620329ed3e848fdd95905a93ca4b3))
* remove extra semicolon ([fb7f55d](https://github.com/openmind-consortium/OpenMindServer/commit/fb7f55d153344d845fa670c9d67d459e5bfea059))
* removed unnecessary text from the return string for Version Number. ([d2723cb](https://github.com/openmind-consortium/OpenMindServer/commit/d2723cbaf98e5ba48cc6e9f96834a75780a07d35))
* semicolon ([3a97872](https://github.com/openmind-consortium/OpenMindServer/commit/3a97872151dee36f1c522cb0efecd2a365470cf1))
* Sensing Configuration Tests not properly creating 8 power band configuration objects. ([f527ba0](https://github.com/openmind-consortium/OpenMindServer/commit/f527ba0175963dccde54da226d83987e043bbdc4))
* Set retries to 0 as default if no retries given from client. Added return from while loop if ctm disposed and retries limit was reached ([fd98578](https://github.com/openmind-consortium/OpenMindServer/commit/fd98578f5ac2c95c2a900bdeac003d2638967dad))
* stop removing unexpectedCTMDisconnectHandler on already disposed Summit ([a2d4c4b](https://github.com/openmind-consortium/OpenMindServer/commit/a2d4c4b7c6dd8a17da3c23328b17ab3aa647a066))
* Summit Service not assigning ratio field ([6b17987](https://github.com/openmind-consortium/OpenMindServer/commit/6b17987635ff95e61c708004f77e94a0eebe5d7b))
* summitserviceinfo object disposes itself after failed reconnect. ([fd80a0b](https://github.com/openmind-consortium/OpenMindServer/commit/fd80a0b3415aee55504e1c9eea909ff3d82f5548))
* test update due to new object response as opposed to past empty() ([21b30ef](https://github.com/openmind-consortium/OpenMindServer/commit/21b30ef08631ca542681e51e2a5d1fd4d78c7bca))
* update generated code to match protos ([7bf65a1](https://github.com/openmind-consortium/OpenMindServer/commit/7bf65a14a0eeff9ab13f052148c22ba3fac8f51d))
* update protobuf locations ([bae292d](https://github.com/openmind-consortium/OpenMindServer/commit/bae292d9c6c53072afa8e41b98bbcec885685cfe))
* update protos ([e211ef9](https://github.com/openmind-consortium/OpenMindServer/commit/e211ef98778f8fe456eb3ea18cbd8588b4ff84eb))
* update protos version ([3b70ffc](https://github.com/openmind-consortium/OpenMindServer/commit/3b70ffcfb938d6d2c1e12896b9452a11d2450eb2))


### Features

* add supported devices endpoint ([99f2573](https://github.com/openmind-consortium/OpenMindServer/commit/99f2573584269dc8f173f465175afa1dc9cea614))
* Added enable and disable streams ([91cdab9](https://github.com/openmind-consortium/OpenMindServer/commit/91cdab96b7c62fb7404ad25b248867cfe86e8173))
* Added sense enables to configure sense ([f231217](https://github.com/openmind-consortium/OpenMindServer/commit/f231217882ebcf84332140b6bdddd6a979dfd65b))
* added VersionNumber functionality to InfoService.cs. It reads the assembly version automatically from the project assemblies. tested using a python script ([2acd25b](https://github.com/openmind-consortium/OpenMindServer/commit/2acd25b158da01d69105e79f436c619ec5164f18))
* Automatic reconnection failures dispose SummitServiceInfo object and remove from Repository ([456a2a8](https://github.com/openmind-consortium/OpenMindServer/commit/456a2a89413d73c810004ae6e0e67e6edf26b7b5))
* configurable host ([2b605b4](https://github.com/openmind-consortium/OpenMindServer/commit/2b605b42b5a692e023ab72952cbe98bc01701fd3))
* impedance tests ([92784cc](https://github.com/openmind-consortium/OpenMindServer/commit/92784cccd66d2afc3d6bafa152d869473aebf162))
* update protos ([cde9c63](https://github.com/openmind-consortium/OpenMindServer/commit/cde9c6355411354a4b56d33c8f42671c5db73b88))

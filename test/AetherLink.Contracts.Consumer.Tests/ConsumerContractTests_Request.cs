using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using AElf.Types;
using AetherLink.Contracts.DataFeeds.Coordinator;
using AetherLink.Contracts.Oracle;
using Coordinator;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Oracle;
using Shouldly;
using Xunit;
using ConfigSet = AetherLink.Contracts.Oracle.ConfigSet;

namespace AetherLink.Contracts.Consumer;

public partial class ConsumerContractTests
{
    [Fact]
    public async Task DataFeedsTests()
    {
        var (subscriptionId, requestTypeIndex) = await PrepareForDataFeedsWithoutUpdateContractsAsync();

        // start request
        Hash requestId;
        Commitment commitment;

        {
            var result = await ConsumerContractStub.StartOracleRequest.SendAsync(new StartOracleRequestInput
            {
                SubscriptionId = subscriptionId,
                RequestTypeIndex = requestTypeIndex,
                SpecificData = new SpecificData
                {
                    Data = ByteString.Empty,
                    DataVersion = 0
                }.ToByteString()
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            {
                var log = GetLogEvent<RequestSent>(result.TransactionResult);
                log.RequestId.ShouldNotBeNull();
                log.RequestingContract.ShouldBe(ConsumerContractAddress);
                log.RequestingInitiator.ShouldBe(DefaultAddress);
                log.Commitment.ShouldNotBeNull();

                requestId = log.RequestId;
                commitment = Commitment.Parser.ParseFrom(log.Commitment);
                commitment.SubscriptionId.ShouldBe(subscriptionId);
                commitment.SpecificData.ShouldNotBeNull();
                commitment.RequestId.ShouldBe(requestId);
                commitment.Coordinator.ShouldBe(DataFeedsCoordinatorContractAddress);
                commitment.Client.ShouldBe(ConsumerContractAddress);
                commitment.RequestTypeIndex.ShouldBe(requestTypeIndex);
                commitment.TimeoutTimestamp.ShouldNotBeNull();

                var specificData = SpecificData.Parser.ParseFrom(commitment.SpecificData);
                specificData.Data.ShouldBe(ByteString.Empty);
                specificData.DataVersion.ShouldBe(0);
            }
            {
                var log = GetLogEvent<RequestStarted>(result.TransactionResult);
                log.RequestId.ShouldBe(requestId);
                log.SubscriptionId.ShouldBe(subscriptionId);
                log.RequestingContract.ShouldBe(ConsumerContractAddress);
                log.RequestingInitiator.ShouldBe(DefaultAddress);
                log.RequestTypeIndex.ShouldBe(requestTypeIndex);
            }
            {
                var log = GetLogEvent<OracleRequestSent>(result.TransactionResult);
                log.SubscriptionId.ShouldBe(subscriptionId);
                log.RequestingContract.ShouldBe(ConsumerContractAddress);
                log.RequestInitiator.ShouldBe(DefaultAddress);
                log.SubscriptionOwner.ShouldBe(DefaultAddress);
            }
            {
                var log = GetLogEvent<OracleRequestStarted>(result.TransactionResult);
                log.SubscriptionId.ShouldBe(subscriptionId);
                log.RequestTypeIndex.ShouldBe(requestTypeIndex);
                log.SpecificData.ShouldBe(new SpecificData
                {
                    Data = ByteString.Empty,
                    DataVersion = 0
                }.ToByteString());
            }
            {
                var output = await OracleContractStub.IsPendingRequestExists.CallAsync(new Int64Value
                {
                    Value = subscriptionId
                });
                output.Value.ShouldBeTrue();
            }
            {
                var output = await OracleContractStub.GetConsumer.CallAsync(new GetConsumerInput
                {
                    SubscriptionId = subscriptionId,
                    Consumer = ConsumerContractAddress
                });
                output.InitiatedRequests.ShouldBe(1);
                output.CompletedRequests.ShouldBe(0);
                output.Allowed.ShouldBeTrue();
            }
        }

        // fulfill request
        var longList = new List<long> { 2, 1, 3, 1, 2 };
        var report = new Report
        {
            Result = new LongList
            {
                Data = { longList }
            }.ToByteString(),
            OnChainMetadata = commitment.ToByteString(),
            Error = ByteString.Empty,
            OffChainMetadata = ByteString.Empty
        }.ToByteString();

        var transmitInput = new TransmitInput
        {
            Report = report
        };

        var config = await OracleContractStub.GetConfig.CallAsync(new Empty());
        transmitInput.ReportContext.Add(config.Config.LatestConfigDigest);
        var round = await OracleContractStub.GetLatestRound.CallAsync(new Empty());
        round.Value.ShouldBe(0);
        transmitInput.ReportContext.Add(HashHelper.ComputeFrom(round.Value));
        transmitInput.ReportContext.Add(HashHelper.ComputeFrom(0));

        {
            var signatures = new List<ByteString>
            {
                GenerateSignature(Signer1KeyPair.PrivateKey, transmitInput),
                GenerateSignature(Signer2KeyPair.PrivateKey, transmitInput),
                GenerateSignature(Signer3KeyPair.PrivateKey, transmitInput),
                GenerateSignature(Signer4KeyPair.PrivateKey, transmitInput),
                GenerateSignature(Signer5KeyPair.PrivateKey, transmitInput)
            };

            transmitInput.Signatures.AddRange(signatures);

            var result = await OracleContractStub.Transmit.SendAsync(transmitInput);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            {
                var log = GetLogEvent<PriceUpdated>(result.TransactionResult);
                log.From.ShouldBe(0);
                log.To.ShouldBe(2);
            }
            {
                var log = GetLogEvent<Transmitted>(result.TransactionResult);
                log.RequestId.ShouldBe(requestId);
                log.Transmitter.ShouldBe(DefaultAddress);
                log.ConfigDigest.ShouldBe(config.Config.LatestConfigDigest);
                log.EpochAndRound.ShouldBe(1);
            }
            {
                var log = GetLogEvent<RequestProcessed>(result.TransactionResult);
                log.RequestId.ShouldBe(requestId);
                log.SubscriptionId.ShouldBe(subscriptionId);
                log.Transmitter.ShouldBe(DefaultAddress);
                log.Response.ShouldNotBeNull();
            }
            {
                var log = GetLogEvent<OracleFulfillmentHandled>(result.TransactionResult);
                log.RequestId.ShouldBe(requestId);
                log.RequestTypeIndex.ShouldBe(requestTypeIndex);
            }
            {
                var log = GetLogEvent<Reported>(result.TransactionResult);
                log.RequestId.ShouldBe(requestId);
                log.Transmitter.ShouldBe(DefaultAddress);
            }
            {
                var log = GetLogEvent<PriceUpdated>(result.TransactionResult);
                log.From.ShouldBe(0);
                log.To.ShouldBe(2);
                log.RoundNumber.ShouldBe(1);
                log.UpdateAt.ShouldNotBeNull();
            }
        }
        {
            var output = await OracleContractStub.GetConsumer.CallAsync(new GetConsumerInput
            {
                SubscriptionId = subscriptionId,
                Consumer = ConsumerContractAddress
            });
            output.InitiatedRequests.ShouldBe(1);
            output.CompletedRequests.ShouldBe(1);
            output.Allowed.ShouldBeTrue();
        }
        {
            var output = await OracleContractStub.GetLatestRound.CallAsync(new Empty());
            output.Value.ShouldBe(1);
        }
        {
            var result = await ConsumerContractStub.GetOracleResponse.CallAsync(requestId);
            result.Response.ShouldNotBeNull();
            result.Err.ShouldBe(ByteString.Empty);
        }
        {
            var result = await ConsumerContractStub.GetPriceList.CallAsync(requestId);
            result.Data.Count.ShouldBe(longList.Count);
        }
        {
            var result = await ConsumerContractStub.GetLatestPriceRoundData.CallAsync(new Empty());
            result.Price.ShouldBe(2);
        }
        {
            var result = await ConsumerContractStub.GetPriceRoundData.CallAsync(new Int64Value
            {
                Value = 1
            });
            result.RoundId.ShouldBe(1);
            result.Price.ShouldBe(2);
            result.UpdatedAt.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task OracleSetConfigTests()
    {
        await DataFeedsTests();

        {
            var output = await OracleContractStub.GetLatestRound.CallAsync(new Empty());
            output.Value.ShouldBe(1);
        }
        var blockNumber = OracleContractStub.GetLatestConfigDetails.CallAsync(new Empty()).Result.BlockNumber;
        var signers = new List<Address>
            { Accounts[10].Address, Accounts[11].Address, Accounts[12].Address, Accounts[13].Address };
        var transmitters = new List<Address>
            { Accounts[14].Address, Accounts[15].Address, Accounts[16].Address, Accounts[17].Address };

        var result = await OracleContractStub.SetConfig.SendAsync(new SetConfigInput
        {
            F = 1,
            Signers = { signers },
            Transmitters = { transmitters }
        });

        var log = GetLogEvent<ConfigSet>(result.TransactionResult);
        log.F.ShouldBe(1);
        log.Signers.Data.ShouldBe(signers);
        log.Transmitters.Data.ShouldBe(transmitters);
        log.ConfigCount.ShouldBe(2);
        log.PreviousConfigBlockNumber.ShouldBe(blockNumber);

        {
            var output = await OracleContractStub.GetOracle.CallAsync(Signer1Address);
            output.ShouldBe(new Oracle.Oracle());
        }
        {
            var output = await OracleContractStub.GetOracle.CallAsync(Transmitter1Address);
            output.ShouldBe(new Oracle.Oracle());
        }
        {
            var output = await OracleContractStub.GetOracle.CallAsync(Accounts[10].Address);
            output.Index.ShouldBe(0);
            output.Role.ShouldBe(Role.Signer);
        }
        {
            var output = await OracleContractStub.GetOracle.CallAsync(Accounts[14].Address);
            output.Index.ShouldBe(0);
            output.Role.ShouldBe(Role.Transmitter);
        }
        {
            var output = await OracleContractStub.GetConfig.CallAsync(new Empty());
            output.Config.F.ShouldBe(1);
            output.Config.N.ShouldBe(signers.Count);
            output.Config.LatestConfigDigest.ShouldBe(log.ConfigDigest);
        }
        {
            var output = await OracleContractStub.GetLatestConfigDetails.CallAsync(new Empty());
            output.ConfigDigest.ShouldBe(log.ConfigDigest);
            output.ConfigCount.ShouldBe(log.ConfigCount);
            output.BlockNumber.ShouldBe(result.TransactionResult.BlockNumber);
        }
        {
            var output = await OracleContractStub.GetTransmitters.CallAsync(new Empty());
            output.Data.ShouldBe(transmitters);
        }
        {
            var output = await OracleContractStub.GetLatestRound.CallAsync(new Empty());
            output.Value.ShouldBe(0);
        }
    }

    [Fact]
    public async Task VrfTests()
    {
        var (subscriptionId, requestTypeIndex) = await PrepareForVrfWithoutUpdateContractsAsync();

        // start request
        Hash requestId;
        Commitment commitment;
        var specificData = new VRF.Coordinator.SpecificData
        {
            KeyHash = HashHelper.ComputeFrom(DefaultKeyPair.PublicKey.ToHex()),
            RequestConfirmations = 0,
            NumWords = 3
        };

        {
            var result = await ConsumerContractStub.StartOracleRequest.SendAsync(new StartOracleRequestInput
            {
                SubscriptionId = subscriptionId,
                RequestTypeIndex = requestTypeIndex,
                SpecificData = specificData.ToByteString()
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            {
                var log = GetLogEvent<RequestSent>(result.TransactionResult);
                log.RequestId.ShouldNotBeNull();
                log.RequestingContract.ShouldBe(ConsumerContractAddress);
                log.RequestingInitiator.ShouldBe(DefaultAddress);
                log.Commitment.ShouldNotBeNull();

                requestId = log.RequestId;
                commitment = Commitment.Parser.ParseFrom(log.Commitment);
                commitment.SubscriptionId.ShouldBe(subscriptionId);
                commitment.SpecificData.ShouldNotBeNull();
                commitment.RequestId.ShouldBe(requestId);
                commitment.Coordinator.ShouldBe(VrfCoordinatorContractAddress);
                commitment.Client.ShouldBe(ConsumerContractAddress);
                commitment.RequestTypeIndex.ShouldBe(requestTypeIndex);
                commitment.TimeoutTimestamp.ShouldNotBeNull();

                specificData = VRF.Coordinator.SpecificData.Parser.ParseFrom(commitment.SpecificData);
                specificData.BlockNumber.ShouldBe(result.TransactionResult.BlockNumber);
                specificData.KeyHash.ShouldBe(HashHelper.ComputeFrom(DefaultKeyPair.PublicKey.ToHex()));
                specificData.RequestConfirmations.ShouldBe(0);
                specificData.NumWords.ShouldBe(3);
                specificData.PreSeed.ShouldNotBeNull();
            }
            {
                var log = GetLogEvent<RequestStarted>(result.TransactionResult);
                log.RequestId.ShouldBe(requestId);
                log.SubscriptionId.ShouldBe(subscriptionId);
                log.RequestingContract.ShouldBe(ConsumerContractAddress);
                log.RequestingInitiator.ShouldBe(DefaultAddress);
                log.RequestTypeIndex.ShouldBe(requestTypeIndex);
            }
            {
                var log = GetLogEvent<OracleRequestStarted>(result.TransactionResult);
                log.SubscriptionId.ShouldBe(subscriptionId);
                log.RequestTypeIndex.ShouldBe(requestTypeIndex);
                log.SpecificData.ShouldBe(new VRF.Coordinator.SpecificData
                {
                    KeyHash = HashHelper.ComputeFrom(DefaultKeyPair.PublicKey.ToHex()),
                    RequestConfirmations = 0,
                    NumWords = 3
                }.ToByteString());
            }
        }

        // fulfill request
        var random = await ConsensusContractStub.GetRandomHash.CallAsync(new Int64Value
        {
            Value = specificData.BlockNumber
        });

        var report = new Report
        {
            Result = ByteString.CopyFrom(CryptoHelper.ECVrfProve(DefaultKeyPair,
                HashHelper.ConcatAndCompute(random, specificData.PreSeed)
                    .ToByteArray())),
            OnChainMetadata = commitment.ToByteString(),
            Error = ByteString.Empty,
            OffChainMetadata = ByteString.Empty
        }.ToByteString();

        var transmitInput = new TransmitInput
        {
            Report = report
        };

        {
            var config = await OracleContractStub.GetConfig.CallAsync(new Empty());
            transmitInput.ReportContext.Add(config.Config.LatestConfigDigest);

            var round = await OracleContractStub.GetLatestRound.CallAsync(new Empty());
            transmitInput.ReportContext.Add(HashHelper.ComputeFrom(round.Value));
            transmitInput.ReportContext.Add(HashHelper.ComputeFrom(0));
        }

        {
            var signatures = new List<ByteString> { GenerateSignature(DefaultKeyPair.PrivateKey, transmitInput) };
            transmitInput.Signatures.AddRange(signatures);

            var result = await OracleContractStub.Transmit.SendAsync(transmitInput);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            {
                var log = GetLogEvent<OracleFulfillmentHandled>(result.TransactionResult);
                log.RequestId.ShouldBe(requestId);
                log.RequestTypeIndex.ShouldBe(requestTypeIndex);
            }
            {
                var log = GetLogEvent<Reported>(result.TransactionResult);
                log.RequestId.ShouldBe(requestId);
                log.Transmitter.ShouldBe(DefaultAddress);
            }
            {
                var output = await ConsumerContractStub.GetOracleResponse.CallAsync(requestId);
                output.Response.ShouldNotBeNull();
                output.Err.ShouldBe(ByteString.Empty);
            }
            {
                var output = await ConsumerContractStub.GetRandomHashList.CallAsync(requestId);
                output.Data.Count.ShouldBe(3);
            }
        }
    }

    [Fact]
    public async Task StartOracleRequestTests_Fail()
    {
        {
            var result =
                await ConsumerContractStub.StartOracleRequest.SendWithExceptionAsync(new StartOracleRequestInput());
            result.TransactionResult.Error.ShouldContain("Controller not set.");
        }

        await InitializeAsync();

        {
            var result =
                await UserConsumerContractStub.StartOracleRequest.SendWithExceptionAsync(new StartOracleRequestInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result =
                await ConsumerContractStub.StartOracleRequest.SendWithExceptionAsync(new StartOracleRequestInput());
            result.TransactionResult.Error.ShouldContain("Oracle contract not set.");
        }

        await ConsumerContractStub.SetOracleContract.SendAsync(OracleContractAddress);

        {
            var result =
                await ConsumerContractStub.StartOracleRequest.SendWithExceptionAsync(new StartOracleRequestInput());
            result.TransactionResult.Error.ShouldContain("Invalid input subscription id.");
        }
        {
            var result =
                await ConsumerContractStub.StartOracleRequest.SendWithExceptionAsync(new StartOracleRequestInput
                {
                    SubscriptionId = 1
                });
            result.TransactionResult.Error.ShouldContain("Invalid request type index.");
        }
    }

    [Fact]
    public async Task HandleOracleFulfillmentTests_Fail()
    {
        await InitializeAsync();
        await ConsumerContractStub.SetOracleContract.SendAsync(DefaultAddress);

        {
            var result =
                await UserConsumerContractStub.HandleOracleFulfillment.SendWithExceptionAsync(
                    new HandleOracleFulfillmentInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result =
                await ConsumerContractStub.HandleOracleFulfillment.SendWithExceptionAsync(
                    new HandleOracleFulfillmentInput());
            result.TransactionResult.Error.ShouldContain("Invalid input request id.");
        }
        {
            var result =
                await ConsumerContractStub.HandleOracleFulfillment.SendWithExceptionAsync(
                    new HandleOracleFulfillmentInput
                    {
                        RequestId = new Hash()
                    });
            result.TransactionResult.Error.ShouldContain("Invalid input request id.");
        }
        {
            var result =
                await ConsumerContractStub.HandleOracleFulfillment.SendWithExceptionAsync(
                    new HandleOracleFulfillmentInput
                    {
                        RequestId = Hash.Empty
                    });
            result.TransactionResult.Error.ShouldContain("Invalid request type index.");
        }
        {
            var result =
                await ConsumerContractStub.HandleOracleFulfillment.SendWithExceptionAsync(
                    new HandleOracleFulfillmentInput
                    {
                        RequestId = Hash.Empty,
                        RequestTypeIndex = 3
                    });
            result.TransactionResult.Error.ShouldContain("Invalid input response or err.");
        }
        {
            var result =
                await ConsumerContractStub.HandleOracleFulfillment.SendWithExceptionAsync(
                    new HandleOracleFulfillmentInput
                    {
                        RequestId = Hash.Empty,
                        RequestTypeIndex = 3,
                        Response = Hash.Empty.Value
                    });
            result.TransactionResult.Error.ShouldContain("Invalid request type index.");
        }
    }

    [Fact]
    public async Task OracleCancelSubscriptionDuringDataFeedsTests()
    {
        var (subscriptionId, requestTypeIndex) = await PrepareForDataFeedsWithoutUpdateContractsAsync();

        await ConsumerContractStub.StartOracleRequest.SendAsync(new StartOracleRequestInput
        {
            SubscriptionId = subscriptionId,
            RequestTypeIndex = requestTypeIndex,
            SpecificData = new SpecificData
            {
                Data = ByteString.Empty,
                DataVersion = 0
            }.ToByteString()
        });
        var output = await OracleContractStub.IsPendingRequestExists.CallAsync(new Int64Value
        {
            Value = subscriptionId
        });
        output.Value.ShouldBeTrue();

        {
            var result = await OracleContractStub.RemoveConsumer.SendWithExceptionAsync(new RemoveConsumerInput
            {
                Consumer = ConsumerContractAddress,
                SubscriptionId = subscriptionId
            });
            result.TransactionResult.Error.ShouldContain("Cannot remove consumer with pending requests.");
        }
        {
            var result = await OracleContractStub.CancelSubscription.SendWithExceptionAsync(new CancelSubscriptionInput
            {
                SubscriptionId = subscriptionId
            });
            result.TransactionResult.Error.ShouldContain("Cannot cancel subscription with pending requests.");
        }
        {
            var result = await OracleContractStub.AdminCancelSubscription.SendAsync(new Int64Value
            {
                Value = subscriptionId
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<SubscriptionCanceled>(result.TransactionResult);
            log.SubscriptionId.ShouldBe(subscriptionId);
        }
    }

    [Fact]
    public async Task OracleCancelSubscriptionDuringVrfTests()
    {
        var (subscriptionId, requestTypeIndex) = await PrepareForVrfWithoutUpdateContractsAsync();

        await ConsumerContractStub.StartOracleRequest.SendAsync(new StartOracleRequestInput
        {
            SubscriptionId = subscriptionId,
            RequestTypeIndex = requestTypeIndex,
            SpecificData = new VRF.Coordinator.SpecificData
            {
                KeyHash = HashHelper.ComputeFrom(DefaultKeyPair.PublicKey.ToHex()),
                RequestConfirmations = 0,
                NumWords = 3
            }.ToByteString()
        });
        var output = await OracleContractStub.IsPendingRequestExists.CallAsync(new Int64Value
        {
            Value = subscriptionId
        });
        output.Value.ShouldBeTrue();

        {
            var result = await OracleContractStub.RemoveConsumer.SendWithExceptionAsync(new RemoveConsumerInput
            {
                Consumer = ConsumerContractAddress,
                SubscriptionId = subscriptionId
            });
            result.TransactionResult.Error.ShouldContain("Cannot remove consumer with pending requests.");
        }
        {
            var result = await OracleContractStub.CancelSubscription.SendWithExceptionAsync(new CancelSubscriptionInput
            {
                SubscriptionId = subscriptionId
            });
            result.TransactionResult.Error.ShouldContain("Cannot cancel subscription with pending requests.");
        }
        {
            var result = await OracleContractStub.AdminCancelSubscription.SendAsync(new Int64Value
            {
                Value = subscriptionId
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<SubscriptionCanceled>(result.TransactionResult);
            log.SubscriptionId.ShouldBe(subscriptionId);
        }
    }

    [Fact]
    public async Task OracleCancelSubscriptionDuringSecondDataFeedsTests()
    {
        var (subscriptionId, requestTypeIndex) = await PrepareForDataFeedsWithoutUpdateContractsAsync();

        // start request
        Hash requestId;
        Commitment commitment;

        {
            var result = await ConsumerContractStub.StartOracleRequest.SendAsync(new StartOracleRequestInput
            {
                SubscriptionId = subscriptionId,
                RequestTypeIndex = requestTypeIndex,
                SpecificData = new SpecificData
                {
                    Data = ByteString.Empty,
                    DataVersion = 0
                }.ToByteString()
            });
            var requestSentLog = GetLogEvent<RequestSent>(result.TransactionResult);
            requestId = requestSentLog.RequestId;
            commitment = Commitment.Parser.ParseFrom(requestSentLog.Commitment);
        }

        var longList = new List<long> { 2, 1, 3, 1, 2 };
        var report = new Report
        {
            Result = new LongList
            {
                Data = { longList }
            }.ToByteString(),
            OnChainMetadata = commitment.ToByteString(),
            Error = ByteString.Empty,
            OffChainMetadata = ByteString.Empty
        }.ToByteString();

        var transmitInput = new TransmitInput
        {
            Report = report
        };

        var config = await OracleContractStub.GetConfig.CallAsync(new Empty());
        transmitInput.ReportContext.Add(config.Config.LatestConfigDigest);
        var round = await OracleContractStub.GetLatestRound.CallAsync(new Empty());
        round.Value.ShouldBe(0);
        transmitInput.ReportContext.Add(HashHelper.ComputeFrom(round.Value));
        transmitInput.ReportContext.Add(HashHelper.ComputeFrom(0));

        {
            var signatures = new List<ByteString>
            {
                GenerateSignature(Signer1KeyPair.PrivateKey, transmitInput),
                GenerateSignature(Signer2KeyPair.PrivateKey, transmitInput),
                GenerateSignature(Signer3KeyPair.PrivateKey, transmitInput),
                GenerateSignature(Signer4KeyPair.PrivateKey, transmitInput),
                GenerateSignature(Signer5KeyPair.PrivateKey, transmitInput)
            };
            transmitInput.Signatures.AddRange(signatures);

            var result = await OracleContractStub.Transmit.SendAsync(transmitInput);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        {
            var result = await OracleContractStub.CancelSubscription.SendAsync(new CancelSubscriptionInput
            {
                SubscriptionId = 1
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        {
            var roundNumber = await OracleContractStub.GetLatestRound.CallAsync(new Empty());
            transmitInput.ReportContext[1] = HashHelper.ComputeFrom(roundNumber.Value);
            var signatures = new List<ByteString>
            {
                GenerateSignature(Signer1KeyPair.PrivateKey, transmitInput),
                GenerateSignature(Signer2KeyPair.PrivateKey, transmitInput),
                GenerateSignature(Signer3KeyPair.PrivateKey, transmitInput),
                GenerateSignature(Signer4KeyPair.PrivateKey, transmitInput),
                GenerateSignature(Signer5KeyPair.PrivateKey, transmitInput)
            };
            transmitInput.Signatures.Clear();
            transmitInput.Signatures.AddRange(signatures);
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(transmitInput);
            result.TransactionResult.Error.ShouldContain("Subscription not found.");
        }
    }

    [Fact]
    public async Task CancelDataFeedsRequestTests()
    {
        var (subscriptionId, requestTypeIndex) = await PrepareForDataFeedsWithoutUpdateContractsAsync();

        // start request
        Hash requestId;

        {
            var result = await ConsumerContractStub.StartOracleRequest.SendAsync(new StartOracleRequestInput
            {
                SubscriptionId = subscriptionId,
                RequestTypeIndex = requestTypeIndex,
                SpecificData = new SpecificData
                {
                    Data = ByteString.Empty,
                    DataVersion = 0
                }.ToByteString()
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            {
                var log = GetLogEvent<RequestSent>(result.TransactionResult);
                requestId = log.RequestId;
            }
        }
        {
            var output = await OracleContractStub.GetConsumer.CallAsync(new GetConsumerInput
            {
                SubscriptionId = subscriptionId,
                Consumer = ConsumerContractAddress
            });
            output.InitiatedRequests.ShouldBe(1);
            output.CompletedRequests.ShouldBe(0);
        }
        {
            var result = await OracleContractStub.CancelRequest.SendAsync(new CancelRequestInput
            {
                SubscriptionId = subscriptionId,
                RequestId = requestId,
                RequestTypeIndex = requestTypeIndex,
                Consumer = ConsumerContractAddress
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            {
                var log = GetLogEvent<RequestCancelled>(result.TransactionResult);
                log.RequestId.ShouldBe(requestId);
            }
            {
                var log = GetLogEvent<CommitmentDeleted>(result.TransactionResult);
                log.RequestId.ShouldBe(requestId);
            }
        }
        {
            var output = await OracleContractStub.GetConsumer.CallAsync(new GetConsumerInput
            {
                SubscriptionId = subscriptionId,
                Consumer = ConsumerContractAddress
            });
            output.InitiatedRequests.ShouldBe(1);
            output.CompletedRequests.ShouldBe(1);
        }
        {
            var output = await DataFeedsContractStub.GetCommitmentHash.CallAsync(requestId);
            output.ShouldBe(new Hash());
        }
    }

    [Fact]
    public async Task CancelVrfRequestTests()
    {
        var (subscriptionId, requestTypeIndex) = await PrepareForVrfWithoutUpdateContractsAsync();

        // start request
        Hash requestId;

        {
            var result = await ConsumerContractStub.StartOracleRequest.SendAsync(new StartOracleRequestInput
            {
                SubscriptionId = subscriptionId,
                RequestTypeIndex = requestTypeIndex,
                SpecificData = new VRF.Coordinator.SpecificData
                {
                    KeyHash = HashHelper.ComputeFrom(DefaultKeyPair.PublicKey.ToHex()),
                    RequestConfirmations = 0,
                    NumWords = 3
                }.ToByteString()
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            {
                var log = GetLogEvent<RequestSent>(result.TransactionResult);
                requestId = log.RequestId;
            }
        }
        {
            var output = await OracleContractStub.GetConsumer.CallAsync(new GetConsumerInput
            {
                SubscriptionId = subscriptionId,
                Consumer = ConsumerContractAddress
            });
            output.InitiatedRequests.ShouldBe(1);
            output.CompletedRequests.ShouldBe(0);
        }
        {
            var result = await OracleContractStub.CancelRequest.SendAsync(new CancelRequestInput
            {
                SubscriptionId = subscriptionId,
                RequestId = requestId,
                RequestTypeIndex = requestTypeIndex,
                Consumer = ConsumerContractAddress
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            {
                var log = GetLogEvent<RequestCancelled>(result.TransactionResult);
                log.RequestId.ShouldBe(requestId);
            }
            {
                var log = GetLogEvent<CommitmentDeleted>(result.TransactionResult);
                log.RequestId.ShouldBe(requestId);
            }
        }
        {
            var output = await OracleContractStub.GetConsumer.CallAsync(new GetConsumerInput
            {
                SubscriptionId = subscriptionId,
                Consumer = ConsumerContractAddress
            });
            output.InitiatedRequests.ShouldBe(1);
            output.CompletedRequests.ShouldBe(1);
        }
    }
}
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using AetherLink.Contracts.Oracle;
using Coordinator;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Oracle;
using Shouldly;
using Xunit;
using InitializeInput = Coordinator.InitializeInput;

namespace AetherLink.Contracts.VRF.Coordinator;

public partial class VrfCoordinatorContractTests
{
    [Fact]
    public async Task SendRequestTests_Fail()
    {
        {
            var result = await CoordinatorContractStub.SendRequest.SendWithExceptionAsync(new Request());
            result.TransactionResult.Error.ShouldContain("Not initialized.");
        }
        await CoordinatorContractStub.Initialize.SendAsync(new InitializeInput
        {
            Oracle = DefaultAddress,
            Admin = DefaultAddress
        });
        {
            var result = await UserCoordinatorContractStub.SendRequest.SendWithExceptionAsync(new Request());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await CoordinatorContractStub.SendRequest.SendWithExceptionAsync(new Request());
            result.TransactionResult.Error.ShouldContain("Invalid input requesting contract.");
        }
        {
            var result = await CoordinatorContractStub.SendRequest.SendWithExceptionAsync(new Request
            {
                RequestingContract = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input requesting contract.");
        }
        {
            var result = await CoordinatorContractStub.SendRequest.SendWithExceptionAsync(new Request
            {
                RequestingContract = DefaultAddress,
                SubscriptionId = -1
            });
            result.TransactionResult.Error.ShouldContain("Invalid input subscription id.");
        }
        {
            var result = await CoordinatorContractStub.SendRequest.SendWithExceptionAsync(new Request
            {
                RequestingContract = DefaultAddress,
                SubscriptionId = 1,
                InitiatedRequests = -1
            });
            result.TransactionResult.Error.ShouldContain("Invalid input initiated requests.");
        }
        {
            var result = await CoordinatorContractStub.SendRequest.SendWithExceptionAsync(new Request
            {
                RequestingContract = DefaultAddress,
                SubscriptionId = 1,
                InitiatedRequests = 0,
                CompletedRequests = -1
            });
            result.TransactionResult.Error.ShouldContain("Invalid input completed requests.");
        }
        {
            var result = await CoordinatorContractStub.SendRequest.SendWithExceptionAsync(new Request
            {
                RequestingContract = DefaultAddress,
                SubscriptionId = 1,
                InitiatedRequests = 0,
                CompletedRequests = 0
            });
            result.TransactionResult.Error.ShouldContain("Invalid input subscription owner.");
        }
        {
            var result = await CoordinatorContractStub.SendRequest.SendWithExceptionAsync(new Request
            {
                RequestingContract = DefaultAddress,
                SubscriptionId = 1,
                InitiatedRequests = 0,
                CompletedRequests = 0,
                SubscriptionOwner = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input subscription owner.");
        }
        {
            var result = await CoordinatorContractStub.SendRequest.SendWithExceptionAsync(new Request
            {
                RequestingContract = DefaultAddress,
                SubscriptionId = 1,
                InitiatedRequests = 0,
                CompletedRequests = 0,
                SubscriptionOwner = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("Invalid extra data num words.");
        }
        {
            var result = await CoordinatorContractStub.SendRequest.SendWithExceptionAsync(new Request
            {
                RequestingContract = DefaultAddress,
                SubscriptionId = 1,
                InitiatedRequests = 0,
                CompletedRequests = 0,
                SubscriptionOwner = DefaultAddress,
                SpecificData = new SpecificData
                {
                    NumWords = 11
                }.ToByteString()
            });
            result.TransactionResult.Error.ShouldContain("Invalid extra data num words.");
        }
        {
            var result = await CoordinatorContractStub.SendRequest.SendWithExceptionAsync(new Request
            {
                RequestingContract = DefaultAddress,
                SubscriptionId = 1,
                InitiatedRequests = 0,
                CompletedRequests = 0,
                SubscriptionOwner = DefaultAddress,
                SpecificData = new SpecificData
                {
                    NumWords = 2,
                    RequestConfirmations = -1
                }.ToByteString()
            });
            result.TransactionResult.Error.ShouldContain("Invalid extra data request confirmations.");
        }
        {
            var result = await CoordinatorContractStub.SendRequest.SendWithExceptionAsync(new Request
            {
                RequestingContract = DefaultAddress,
                SubscriptionId = 1,
                InitiatedRequests = 0,
                CompletedRequests = 0,
                SubscriptionOwner = DefaultAddress,
                SpecificData = new SpecificData
                {
                    NumWords = 2,
                    RequestConfirmations = 11
                }.ToByteString()
            });
            result.TransactionResult.Error.ShouldContain("Invalid extra data request confirmations.");
        }
        {
            var result = await CoordinatorContractStub.SendRequest.SendWithExceptionAsync(new Request
            {
                RequestingContract = DefaultAddress,
                SubscriptionId = 1,
                InitiatedRequests = 0,
                CompletedRequests = 0,
                SubscriptionOwner = DefaultAddress,
                SpecificData = new SpecificData
                {
                    NumWords = 2,
                    RequestConfirmations = 2
                }.ToByteString()
            });
            result.TransactionResult.Error.ShouldContain("Invalid extra data key hash.");
        }
        {
            var result = await CoordinatorContractStub.SendRequest.SendWithExceptionAsync(new Request
            {
                RequestingContract = DefaultAddress,
                SubscriptionId = 1,
                InitiatedRequests = 0,
                CompletedRequests = 0,
                SubscriptionOwner = DefaultAddress,
                SpecificData = new SpecificData
                {
                    NumWords = 2,
                    RequestConfirmations = 2,
                    KeyHash = new Hash()
                }.ToByteString()
            });
            result.TransactionResult.Error.ShouldContain("Invalid extra data key hash.");
        }

        await CoordinatorContractStub.Pause.SendAsync(new Empty());
        {
            var result = await CoordinatorContractStub.SendRequest.SendWithExceptionAsync(new Request());
            result.TransactionResult.Error.ShouldContain("Contract paused.");
        }
    }

    [Fact]
    public async Task ReportTests_Fail()
    {
        {
            var result = await CoordinatorContractStub.Report.SendWithExceptionAsync(new ReportInput());
            result.TransactionResult.Error.ShouldContain("Not initialized.");
        }

        await CoordinatorContractStub.Initialize.SendAsync(new InitializeInput
        {
            Oracle = DefaultAddress,
            Admin = DefaultAddress,
            RequestTypeIndex = 1
        });

        {
            var result = await UserCoordinatorContractStub.Report.SendWithExceptionAsync(new ReportInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await CoordinatorContractStub.Report.SendWithExceptionAsync(new ReportInput());
            result.TransactionResult.Error.ShouldContain("Invalid input transmitter.");
        }
        {
            var result = await CoordinatorContractStub.Report.SendWithExceptionAsync(new ReportInput
            {
                Transmitter = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input transmitter.");
        }
        {
            var result = await CoordinatorContractStub.Report.SendWithExceptionAsync(new ReportInput
            {
                Transmitter = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("Invalid input report context.");
        }
        {
            var result = await CoordinatorContractStub.Report.SendWithExceptionAsync(new ReportInput
            {
                Transmitter = DefaultAddress,
                ReportContext = { new Hash(), new Hash() }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input report context.");
        }
        {
            var result = await CoordinatorContractStub.Report.SendWithExceptionAsync(new ReportInput
            {
                Transmitter = DefaultAddress,
                ReportContext = { new Hash(), new Hash(), new Hash() }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input config digest.");
        }
        {
            var result = await CoordinatorContractStub.Report.SendWithExceptionAsync(new ReportInput
            {
                Transmitter = DefaultAddress,
                ReportContext = { Hash.Empty, new Hash(), new Hash() }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input epochAndRound.");
        }
        {
            var result = await CoordinatorContractStub.Report.SendWithExceptionAsync(new ReportInput
            {
                Transmitter = DefaultAddress,
                ReportContext = { Hash.Empty, Hash.Empty, new Hash() }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input report.");
        }
        {
            var result = await CoordinatorContractStub.Report.SendWithExceptionAsync(new ReportInput
            {
                Transmitter = DefaultAddress,
                ReportContext = { Hash.Empty, Hash.Empty, new Hash() },
                Report = ByteString.Empty
            });
            result.TransactionResult.Error.ShouldContain("Invalid input report.");
        }
        {
            var result = await CoordinatorContractStub.Report.SendWithExceptionAsync(new ReportInput
            {
                Transmitter = DefaultAddress,
                ReportContext = { Hash.Empty, Hash.Empty, new Hash() },
                Report = Hash.Empty.Value
            });
            result.TransactionResult.Error.ShouldContain("Invalid input signature.");
        }
        {
            var result = await CoordinatorContractStub.Report.SendWithExceptionAsync(new ReportInput
            {
                Transmitter = DefaultAddress,
                ReportContext = { Hash.Empty, Hash.Empty, new Hash() },
                Report = new Report
                {
                    Result = ByteString.Empty,
                    Error = ByteString.Empty,
                    OnChainMetadata = new Commitment
                    {
                        Coordinator = DefaultAddress
                    }.ToByteString(),
                }.ToByteString(),
                Signatures = { Hash.Empty.Value }
            });
            result.TransactionResult.Error.ShouldContain("Invalid report response or err.");
        }
        {
            var result = await CoordinatorContractStub.Report.SendWithExceptionAsync(new ReportInput
            {
                Transmitter = DefaultAddress,
                ReportContext = { Hash.Empty, Hash.Empty, new Hash() },
                Report = new Report
                {
                    Result = Hash.Empty.Value,
                    Error = ByteString.Empty,
                    OnChainMetadata = ByteString.Empty,
                }.ToByteString(),
                Signatures = { Hash.Empty.Value }
            });
            result.TransactionResult.Error.ShouldContain("Invalid report on chain metadata.");
        }
        {
            var result = await CoordinatorContractStub.Report.SendWithExceptionAsync(new ReportInput
            {
                Transmitter = DefaultAddress,
                ReportContext = { Hash.Empty, Hash.Empty, new Hash() },
                Report = new Report
                {
                    Result = Hash.Empty.Value,
                    Error = ByteString.Empty,
                    OnChainMetadata = new Commitment
                    {
                        RequestId = new Hash()
                    }.ToByteString(),
                }.ToByteString(),
                Signatures = { Hash.Empty.Value }
            });
            result.TransactionResult.Error.ShouldContain("Invalid commitment request id.");
        }
        {
            var result = await CoordinatorContractStub.Report.SendWithExceptionAsync(new ReportInput
            {
                Transmitter = DefaultAddress,
                ReportContext = { Hash.Empty, Hash.Empty, new Hash() },
                Report = new Report
                {
                    Result = Hash.Empty.Value,
                    Error = ByteString.Empty,
                    OnChainMetadata = new Commitment
                    {
                        RequestId = Hash.Empty
                    }.ToByteString(),
                }.ToByteString(),
                Signatures = { Hash.Empty.Value }
            });
            result.TransactionResult.Error.ShouldContain("Invalid request id.");
        }

        await CoordinatorContractStub.SetOracleContractAddress.SendAsync(OracleContractAddress);
        await OracleContractStub.Initialize.SendAsync(new Oracle.InitializeInput
        {
            Admin = DefaultAddress
        });
        await OracleContractStub.AddCoordinator.SendAsync(VrfCoordinatorContractAddress);
        ByteString commitment;
        Hash requestId;
        await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(DefaultAddress);
        await OracleContractStub.RegisterProvingKey.SendAsync(new RegisterProvingKeyInput
        {
            PublicProvingKey = DefaultKeyPair.PublicKey.ToHex(),
            Oracle = DefaultAddress
        });

        {
            var result = await OracleContractStub.SendRequest.SendAsync(new SendRequestInput
            {
                RequestTypeIndex = 1,
                SpecificData = new SpecificData
                {
                    KeyHash = HashHelper.ComputeFrom(DefaultKeyPair.PublicKey.ToHex()),
                    NumWords = 1,
                    RequestConfirmations = 0
                }.ToByteString(),
                SubscriptionId = 1
            });

            var log = GetLogEvent<RequestSent>(result.TransactionResult);
            commitment = log.Commitment;
            requestId = log.RequestId;
        }

        await OracleContractStub.SetConfig.SendAsync(new SetConfigInput
        {
            F = 1,
            Signers = { Accounts[2].Address, Accounts[3].Address, Accounts[4].Address, Accounts[5].Address },
            Transmitters = { DefaultAddress, Accounts[7].Address, Accounts[8].Address, Accounts[9].Address }
        });
        var config = await OracleContractStub.GetLatestConfigDetails.CallAsync(new Empty());
        var round = await OracleContractStub.GetLatestRound.CallAsync(new Empty());

        await CoordinatorContractStub.SetOracleContractAddress.SendAsync(OracleContractAddress);

        var commitmentWithWrongCommitment = Commitment.Parser.ParseFrom(commitment);
        commitmentWithWrongCommitment.RequestTypeIndex = 5;

        var transmitInput = new TransmitInput
        {
            ReportContext = { config.ConfigDigest, HashHelper.ComputeFrom(round.Value), new Hash() },
            Report = new Report
            {
                Result = Hash.Empty.Value,
                Error = ByteString.Empty,
                OnChainMetadata = commitmentWithWrongCommitment.ToByteString(),
                OffChainMetadata = ByteString.Empty
            }.ToByteString(),
        };
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput
            {
                ReportContext = { transmitInput.ReportContext },
                Report = transmitInput.Report,
                Signatures = { Hash.Empty.Value }
            });
            result.TransactionResult.Error.ShouldContain("Invalid commitment.");
        }

        transmitInput = new TransmitInput
        {
            ReportContext = { config.ConfigDigest, HashHelper.ComputeFrom(round.Value), new Hash() },
            Report = new Report
            {
                Result = Hash.Empty.Value,
                Error = ByteString.Empty,
                OnChainMetadata = commitment,
                OffChainMetadata = ByteString.Empty
            }.ToByteString(),
        };
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput
            {
                ReportContext = { transmitInput.ReportContext },
                Report = transmitInput.Report,
                Signatures = { Hash.Empty.Value }
            });
            result.TransactionResult.Error.ShouldContain("Invalid signature.");
        }
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput
            {
                ReportContext = { transmitInput.ReportContext },
                Report = transmitInput.Report,
                Signatures = { GenerateSignature(UserKeyPair.PrivateKey, transmitInput) }
            });
            result.TransactionResult.Error.ShouldContain("Invalid public key.");
        }
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput
            {
                ReportContext = { transmitInput.ReportContext },
                Report = transmitInput.Report,
                Signatures = { GenerateSignature(DefaultKeyPair.PrivateKey, transmitInput) }
            });
            result.TransactionResult.Error.ShouldContain("Vrf verification fail.");
        }

        {
            var result = await OracleContractStub.SendRequest.SendAsync(new SendRequestInput
            {
                RequestTypeIndex = 1,
                SpecificData = new SpecificData
                {
                    KeyHash = HashHelper.ComputeFrom(DefaultKeyPair.PublicKey.ToHex()),
                    NumWords = 1,
                    RequestConfirmations = 10
                }.ToByteString(),
                SubscriptionId = 1
            });

            var log = GetLogEvent<RequestSent>(result.TransactionResult);
            commitment = log.Commitment;
            requestId = log.RequestId;
        }

        transmitInput = new TransmitInput
        {
            ReportContext = { config.ConfigDigest, HashHelper.ComputeFrom(round.Value), new Hash() },
            Report = new Report
            {
                Result = Hash.Empty.Value,
                Error = ByteString.Empty,
                OnChainMetadata = commitment,
                OffChainMetadata = ByteString.Empty
            }.ToByteString(),
        };
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput
            {
                ReportContext = { transmitInput.ReportContext },
                Report = transmitInput.Report,
                Signatures = { GenerateSignature(DefaultKeyPair.PrivateKey, transmitInput) }
            });
            result.TransactionResult.Error.ShouldContain("Not wait enough confirmations.");
        }

        await CoordinatorContractStub.Pause.SendAsync(new Empty());

        {
            var result = await CoordinatorContractStub.Report.SendWithExceptionAsync(new ReportInput());
            result.TransactionResult.Error.ShouldContain("Contract paused.");
        }
    }

    [Fact]
    public async Task DeleteCommitmentTests_Fail()
    {
        {
            var result = await CoordinatorContractStub.DeleteCommitment.SendWithExceptionAsync(Hash.Empty);
            result.TransactionResult.Error.ShouldContain("Not initialized.");
        }

        await InitializeAsync();

        {
            var result = await CoordinatorContractStub.DeleteCommitment.SendWithExceptionAsync(Hash.Empty);
            result.TransactionResult.Error.ShouldContain("No permission.");
        }

        await CoordinatorContractStub.SetOracleContractAddress.SendAsync(DefaultAddress);

        {
            var result = await CoordinatorContractStub.DeleteCommitment.SendWithExceptionAsync(Hash.Empty);
            result.TransactionResult.Error.ShouldContain("Request id not found.");
        }

        await CoordinatorContractStub.Pause.SendAsync(new Empty());

        {
            var result = await CoordinatorContractStub.DeleteCommitment.SendWithExceptionAsync(Hash.Empty);
            result.TransactionResult.Error.ShouldContain("Contract paused.");
        }
    }
}
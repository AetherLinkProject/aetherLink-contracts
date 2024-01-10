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

namespace AetherLink.Contracts.DataFeeds.Coordinator;

public partial class DataFeedsCoordinatorContractTests
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
                RequestingContract = DefaultAddress
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
        await OracleContractStub.AddCoordinator.SendAsync(DataFeedsCoordinatorContractAddress);
        ByteString commitment;
        Hash requestId;
        await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(DefaultAddress);
        {
            var result = await OracleContractStub.SendRequest.SendAsync(new SendRequestInput
            {
                RequestTypeIndex = 1,
                SpecificData = new SpecificData
                {
                    Data = Hash.Empty.Value,
                    DataVersion = 0
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

        var commitmentWithWrongContent = Commitment.Parser.ParseFrom(commitment);
        commitmentWithWrongContent.SubscriptionId = 2;
        var transmitInput = new TransmitInput
        {
            ReportContext = { config.ConfigDigest, HashHelper.ComputeFrom(round.Value), new Hash() },
            Report = new Report
            {
                Result = Hash.Empty.Value,
                Error = ByteString.Empty,
                OnChainMetadata = commitmentWithWrongContent.ToByteString(),
                OffChainMetadata = ByteString.Empty
            }.ToByteString(),
        };

        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput
            {
                ReportContext = { transmitInput.ReportContext },
                Report = transmitInput.Report,
                Signatures = { Hash.Empty.Value, Hash.Empty.Value }
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
                Signatures = { Hash.Empty.Value, Hash.Empty.Value }
            });
            result.TransactionResult.Error.ShouldContain("Not enough signatures.");
        }
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput
            {
                ReportContext = { transmitInput.ReportContext },
                Report = transmitInput.Report,
                Signatures =
                {
                    GenerateSignature(Accounts[2].KeyPair.PrivateKey, transmitInput),
                    GenerateSignature(Accounts[3].KeyPair.PrivateKey, transmitInput),
                    GenerateSignature(Accounts[4].KeyPair.PrivateKey, transmitInput),
                    GenerateSignature(Accounts[5].KeyPair.PrivateKey, transmitInput),
                    Hash.Empty.Value
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid signature.");
        }
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput
            {
                ReportContext = { transmitInput.ReportContext },
                Report = transmitInput.Report,
                Signatures =
                {
                    GenerateSignature(Accounts[2].KeyPair.PrivateKey, transmitInput),
                    GenerateSignature(Accounts[3].KeyPair.PrivateKey, transmitInput),
                    GenerateSignature(Accounts[4].KeyPair.PrivateKey, transmitInput),
                    GenerateSignature(Accounts[5].KeyPair.PrivateKey, transmitInput),
                    GenerateSignature(Accounts[6].KeyPair.PrivateKey, transmitInput)
                }
            });
            result.TransactionResult.Error.ShouldContain("Unauthorized signer.");
        }
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput
            {
                ReportContext = { transmitInput.ReportContext },
                Report = transmitInput.Report,
                Signatures =
                {
                    GenerateSignature(Accounts[2].KeyPair.PrivateKey, transmitInput),
                    GenerateSignature(Accounts[3].KeyPair.PrivateKey, transmitInput),
                    GenerateSignature(Accounts[4].KeyPair.PrivateKey, transmitInput),
                    GenerateSignature(DefaultKeyPair.PrivateKey, transmitInput)
                }
            });
            result.TransactionResult.Error.ShouldContain("Unauthorized signer.");
        }
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput
            {
                ReportContext = { transmitInput.ReportContext },
                Report = transmitInput.Report,
                Signatures =
                {
                    GenerateSignature(Accounts[3].KeyPair.PrivateKey, transmitInput),
                    GenerateSignature(Accounts[3].KeyPair.PrivateKey, transmitInput),
                    GenerateSignature(Accounts[4].KeyPair.PrivateKey, transmitInput),
                    GenerateSignature(Accounts[5].KeyPair.PrivateKey, transmitInput)
                }
            });
            result.TransactionResult.Error.ShouldContain("Duplicate signature.");
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
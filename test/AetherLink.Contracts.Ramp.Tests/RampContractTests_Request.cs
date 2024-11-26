using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using AetherLink.Contracts.Oracle;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AetherLink.Contracts.Ramp;

public partial class RampContractTests
{
    [Fact]
    public async Task SendTests()
    {
        await PrepareOracleContractsAsync();

        {
            var sendInput = new SendInput
            {
                TargetChainId = 1,
                Receiver = UserAddress.ToByteString(),
            };
            var message = HashHelper.ComputeFrom(sendInput).ToByteString();
            sendInput.Message = message;

            var result = await UserRampContractStub.Send.SendAsync(sendInput);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            {
                var log = GetLogEvent<SendRequested>(result.TransactionResult);
                log.Message.ShouldBe(message);
                log.Epoch.ShouldBe(0);
                log.TargetChainId.ShouldBe(1);
                log.Receiver.ShouldBe(UserAddress.ToByteString());
                log.Sender.ShouldBe(UserAddress);
            }
        }

        var messageId = HashHelper.ComputeFrom("test_message");
        var messageData = HashHelper.ComputeFrom("test_message_data").ToByteString();
        var report = new Report
        {
            ReportContext = new ReportContext
            {
                MessageId = messageId,
                SourceChainId = 1100,
                TargetChainId = 9992731,
                Sender = UserAddress.ToByteString(),
                Receiver = TestRampContractAddress.ToByteString()
            },
            Message = messageData
        };

        var commitInput = new CommitInput { Report = report };

        {
            var signatures = new List<ByteString>
            {
                GenerateSignature(Signer1KeyPair.PrivateKey, commitInput),
                GenerateSignature(Signer2KeyPair.PrivateKey, commitInput),
                GenerateSignature(Signer3KeyPair.PrivateKey, commitInput),
                GenerateSignature(Signer4KeyPair.PrivateKey, commitInput),
                GenerateSignature(Signer5KeyPair.PrivateKey, commitInput)
            };

            commitInput.Signatures.AddRange(signatures);
            var result = await TransmitterRampContractStub.Commit.SendAsync(commitInput);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            {
                var log = GetLogEvent<CommitReportAccepted>(result.TransactionResult);
                log.Report.ReportContext.MessageId.ShouldBe(messageId);
                log.Report.ReportContext.SourceChainId.ShouldBe(1100);
                log.Report.ReportContext.TargetChainId.ShouldBe(9992731);
                log.Report.ReportContext.Receiver.ShouldBe(TestRampContractAddress.ToByteString());
                log.Report.ReportContext.Sender.ShouldBe(UserAddress.ToByteString());
                log.Report.Message.ShouldBe(messageData);
            }
        }
    }

    [Fact]
    public async Task SendTokenTests()
    {
        await PrepareOracleContractsAsync();

        {
            var sendInput = new SendInput
            {
                TargetChainId = 1100,
                Receiver = UserAddress.ToByteString(),
                TokenAmount = new()
                {
                    TargetChainId = 1100,
                    TargetContractAddress = "ABC",
                    TokenAddress = "ABC",
                    OriginToken = "ELF",
                    Amount = 100
                }
            };
            var message = HashHelper.ComputeFrom(sendInput).ToByteString();
            sendInput.Message = message;

            var result = await UserRampContractStub.Send.SendAsync(sendInput);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            {
                var log = GetLogEvent<SendRequested>(result.TransactionResult);
                log.Message.ShouldBe(message);
                log.Epoch.ShouldBe(0);
                log.TargetChainId.ShouldBe(1100);
                log.Receiver.ShouldBe(UserAddress.ToByteString());
                log.Sender.ShouldBe(UserAddress);
                log.TokenAmount.TargetChainId.ShouldBe(1100);
                log.TokenAmount.TokenAddress.ShouldBe("ABC");
                log.TokenAmount.OriginToken.ShouldBe("ELF");
                log.TokenAmount.TargetContractAddress.ShouldBe("ABC");
                log.TokenAmount.Amount.ShouldBe(100);
            }
        }

        var messageId = HashHelper.ComputeFrom("test_message");
        var messageData = HashHelper.ComputeFrom("test_message_data").ToByteString();
        var report = new Report
        {
            ReportContext = new ReportContext
            {
                MessageId = messageId,
                SourceChainId = 1100,
                TargetChainId = 9992731,
                Sender = UserAddress.ToByteString(),
                Receiver = TestRampContractAddress.ToByteString()
            },
            Message = messageData,
            TokenAmount = new()
            {
                SwapId = "AAAA",
                TargetChainId = 1100,
                TargetContractAddress = "ABC",
                TokenAddress = "ABC",
                OriginToken = "ELF",
                Amount = 100
            }
        };

        var commitInput = new CommitInput { Report = report };

        {
            var signatures = new List<ByteString>
            {
                GenerateSignature(Signer1KeyPair.PrivateKey, commitInput),
                GenerateSignature(Signer2KeyPair.PrivateKey, commitInput),
                GenerateSignature(Signer3KeyPair.PrivateKey, commitInput),
                GenerateSignature(Signer4KeyPair.PrivateKey, commitInput),
                GenerateSignature(Signer5KeyPair.PrivateKey, commitInput)
            };

            commitInput.Signatures.AddRange(signatures);
            var result = await TransmitterRampContractStub.Commit.SendAsync(commitInput);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            {
                var log = GetLogEvent<CommitReportAccepted>(result.TransactionResult);
                log.Report.ReportContext.MessageId.ShouldBe(messageId);
                log.Report.ReportContext.SourceChainId.ShouldBe(1100);
                log.Report.ReportContext.TargetChainId.ShouldBe(9992731);
                log.Report.ReportContext.Receiver.ShouldBe(TestRampContractAddress.ToByteString());
                log.Report.ReportContext.Sender.ShouldBe(UserAddress.ToByteString());
                log.Report.Message.ShouldBe(messageData);
            }
        }
    }

    [Fact]
    public async Task SendTests_Fail()
    {
        {
            var result = await RampContractStub.Send.SendWithExceptionAsync(new SendInput());
            result.TransactionResult.Error.ShouldContain("Not initialized.");
        }
        await RampContractStub.Initialize.SendAsync(new InitializeInput
        {
            Oracle = DefaultAddress,
            Admin = DefaultAddress
        });

        {
            var result = await UserRampContractStub.Send.SendWithExceptionAsync(new());
            result.TransactionResult.Error.ShouldContain("Invalid sender.");
        }

        await RampContractStub.AddRampSender.SendAsync(new() { SenderAddress = UserAddress });
        await RampContractStub.SetConfig.SendAsync(new() { ChainIdList = new() { Data = { 1 } } });
        {
            var result = await UserRampContractStub.Send.SendWithExceptionAsync(new() { TargetChainId = 11 });
            result.TransactionResult.Error.ShouldContain("Not support target chain.");
        }

        {
            var result = await UserRampContractStub.Send.SendWithExceptionAsync(new() { TargetChainId = 1 });
            result.TransactionResult.Error.ShouldContain("Invalid receiver.");
        }

        {
            var result = await UserRampContractStub.Send.SendWithExceptionAsync(new()
                { TargetChainId = 1, Receiver = DefaultAddress.ToByteString() });
            result.TransactionResult.Error.ShouldContain("Can't cross chain transfer empty message.");
        }

        {
            var result = await UserRampContractStub.Send.SendWithExceptionAsync(new()
            {
                TargetChainId = 1, Receiver = DefaultAddress.ToByteString(),
                Message = HashHelper.ComputeFrom("abcdefghijklmnopqrstuvwxyz").ToByteString(),
                TokenAmount = new()
                {
                    TargetChainId = 0
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid target chainId.");
        }
        
        {
            var result = await UserRampContractStub.Send.SendWithExceptionAsync(new()
            {
                TargetChainId = 1, Receiver = DefaultAddress.ToByteString(),
                Message = HashHelper.ComputeFrom("abcdefghijklmnopqrstuvwxyz").ToByteString(),
                TokenAmount = new()
                {
                    TargetChainId = 11
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid target chainId.");
        }

        {
            var result = await UserRampContractStub.Send.SendWithExceptionAsync(new()
            {
                TargetChainId = 1, Receiver = DefaultAddress.ToByteString(),
                Message = HashHelper.ComputeFrom("abcdefghijklmnopqrstuvwxyz").ToByteString(),
                TokenAmount = new()
                {
                    TargetChainId = 1,
                    OriginToken = ""
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid OriginToken.");
        }

        {
            var result = await UserRampContractStub.Send.SendWithExceptionAsync(new()
            {
                TargetChainId = 1, Receiver = DefaultAddress.ToByteString(),
                Message = HashHelper.ComputeFrom("abcdefghijklmnopqrstuvwxyz").ToByteString(),
                TokenAmount = new()
                {
                    TargetChainId = 1,
                    OriginToken = "ELFUSDT",
                    TokenAddress = "ABC"
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid TargetContractAddress.");
        }
    }

    [Fact]
    public async Task CommitTests_Fail()
    {
        {
            var result = await RampContractStub.Commit.SendWithExceptionAsync(new());
            result.TransactionResult.Error.ShouldContain("Not initialized.");
        }
        await RampContractStub.Initialize.SendAsync(new InitializeInput
        {
            Oracle = DefaultAddress,
            Admin = DefaultAddress
        });

        {
            var result = await UserRampContractStub.Commit.SendWithExceptionAsync(new());
            result.TransactionResult.Error.ShouldContain("Invalid report input.");
        }
        {
            var result = await UserRampContractStub.Commit.SendWithExceptionAsync(new() { Report = new() });
            result.TransactionResult.Error.ShouldContain("Invalid report context.");
        }
        {
            var result = await UserRampContractStub.Commit.SendWithExceptionAsync(new()
                { Report = new() { ReportContext = new() } });
            result.TransactionResult.Error.ShouldContain("Invalid message id.");
        }
        {
            var result = await UserRampContractStub.Commit.SendWithExceptionAsync(new()
                { Report = new() { ReportContext = new() { MessageId = HashHelper.ComputeFrom("mock_message_id") } } });
            result.TransactionResult.Error.ShouldContain("Unmatched chain id.");
        }

        {
            var result = await UserRampContractStub.Commit.SendWithExceptionAsync(new()
            {
                Report = new()
                {
                    ReportContext = new()
                        { MessageId = HashHelper.ComputeFrom("mock_message_id"), TargetChainId = 9992731 }
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid receiver address.");
        }

        await RampContractStub.SetOracleContractAddress.SendAsync(OracleContractAddress);
        await OracleContractStub.Initialize.SendAsync(new() { Admin = DefaultAddress });
        await OracleContractStub.SetConfig.SendAsync(new SetConfigInput
        {
            F = 1,
            Signers = { Accounts[2].Address, Accounts[3].Address, Accounts[4].Address, Accounts[5].Address },
            Transmitters = { DefaultAddress, Transmitter1Address, Transmitter2Address, Transmitter3Address }
        });

        var reportContext = new ReportContext
        {
            MessageId = HashHelper.ComputeFrom("mock_message_id"), TargetChainId = 9992731,
            Receiver = UserAddress.ToByteString()
        };

        {
            var result =
                await UserRampContractStub.Commit.SendWithExceptionAsync(new()
                {
                    Report = new() { ReportContext = reportContext }
                });
            result.TransactionResult.Error.ShouldContain("Invalid transmitter");
        }

        var commitInput = new CommitInput { Report = new() { ReportContext = reportContext } };
        {
            var result = await RampContractStub.Commit.SendWithExceptionAsync(new CommitInput
            {
                Report = new()
                {
                    ReportContext = reportContext,
                },
                Signatures = { Hash.Empty.Value, Hash.Empty.Value }
            });
            result.TransactionResult.Error.ShouldContain("Not enough signatures.");
        }
        {
            var result = await RampContractStub.Commit.SendWithExceptionAsync(new CommitInput
            {
                Report = new() { ReportContext = reportContext },
                Signatures =
                {
                    GenerateSignature(Accounts[2].KeyPair.PrivateKey, commitInput),
                    GenerateSignature(Accounts[3].KeyPair.PrivateKey, commitInput),
                    GenerateSignature(Accounts[4].KeyPair.PrivateKey, commitInput),
                    GenerateSignature(Accounts[5].KeyPair.PrivateKey, commitInput),
                    Hash.Empty.Value
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid signature.");
        }
        {
            var result = await RampContractStub.Commit.SendWithExceptionAsync(new CommitInput
            {
                Report = new() { ReportContext = reportContext },
                Signatures =
                {
                    GenerateSignature(Accounts[2].KeyPair.PrivateKey, commitInput),
                    GenerateSignature(Accounts[3].KeyPair.PrivateKey, commitInput),
                    GenerateSignature(Accounts[4].KeyPair.PrivateKey, commitInput),
                    GenerateSignature(Accounts[5].KeyPair.PrivateKey, commitInput),
                    GenerateSignature(Accounts[6].KeyPair.PrivateKey, commitInput)
                }
            });
            result.TransactionResult.Error.ShouldContain("Unauthorized signer.");
        }
        {
            var result = await RampContractStub.Commit.SendWithExceptionAsync(new CommitInput
            {
                Report = new() { ReportContext = reportContext },
                Signatures =
                {
                    GenerateSignature(Accounts[2].KeyPair.PrivateKey, commitInput),
                    GenerateSignature(Accounts[3].KeyPair.PrivateKey, commitInput),
                    GenerateSignature(Accounts[4].KeyPair.PrivateKey, commitInput),
                    GenerateSignature(DefaultKeyPair.PrivateKey, commitInput)
                }
            });
            result.TransactionResult.Error.ShouldContain("Unauthorized signer.");
        }
        {
            var result = await RampContractStub.Commit.SendWithExceptionAsync(new CommitInput
            {
                Report = new() { ReportContext = reportContext },
                Signatures =
                {
                    GenerateSignature(Accounts[2].KeyPair.PrivateKey, commitInput),
                    GenerateSignature(Accounts[3].KeyPair.PrivateKey, commitInput),
                    GenerateSignature(Accounts[3].KeyPair.PrivateKey, commitInput),
                    GenerateSignature(Accounts[4].KeyPair.PrivateKey, commitInput),
                }
            });
            result.TransactionResult.Error.ShouldContain("Duplicate signature.");
        }
    }
}
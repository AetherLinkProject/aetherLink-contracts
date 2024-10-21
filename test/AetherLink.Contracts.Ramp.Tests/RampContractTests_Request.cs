using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.CSharp.Core;
using AElf.Types;
using AetherLink.Contracts.Oracle;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Asn1.Ocsp;
using Shouldly;
using Xunit;

namespace AetherLink.Contracts.Ramp;

public partial class RampContractTests
{
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
            {
                var result = await UserRampContractStub.Send.SendWithExceptionAsync(new() { TargetChainId = 11 });
                result.TransactionResult.Error.ShouldContain("Not support target chain.");
            }
        }

        {
            {
                var result = await UserRampContractStub.Send.SendWithExceptionAsync(new() { TargetChainId = 1 });
                result.TransactionResult.Error.ShouldContain("Invalid receiver.");
            }
        }

        {
            {
                var result =
                    await UserRampContractStub.Send.SendWithExceptionAsync(new()
                        { TargetChainId = 1, Receiver = DefaultAddress.ToByteString() });
                result.TransactionResult.Error.ShouldContain("Can't cross chain transfer empty message.");
            }
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
            result.TransactionResult.Error.ShouldContain("Invalid report context.");
        }
        {
            var result = await UserRampContractStub.Commit.SendWithExceptionAsync(new() { Report = new() });
            result.TransactionResult.Error.ShouldContain("Invalid message id.");
        }
        {
            var result = await UserRampContractStub.Commit.SendWithExceptionAsync(new()
                { Report = new() { ReportContext = { MessageId = HashHelper.ComputeFrom("mock_message_id") } } });
            result.TransactionResult.Error.ShouldContain("Unmatched chain id.");
        }

        {
            var result = await UserRampContractStub.Commit.SendWithExceptionAsync(new()
            {
                Report =
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
            Transmitters = { DefaultAddress, Accounts[7].Address, Accounts[8].Address, Accounts[9].Address }
        });

        var reportContext = new ReportContext
        {
            MessageId = HashHelper.ComputeFrom("mock_message_id"), TargetChainId = 9992731,
            Receiver = ByteString.CopyFrom(UserAddress.ToByteArray())
        };

        {
            var result =
                await UserRampContractStub.Commit.SendWithExceptionAsync(new()
                {
                    Report =
                    {
                        ReportContext = reportContext
                    }
                });
            result.TransactionResult.Error.ShouldContain("Invalid transmitter");
        }

        var commitInput = new CommitInput { Report = { ReportContext = reportContext } };
        {
            var result = await RampContractStub.Commit.SendWithExceptionAsync(new CommitInput
            {
                Report =
                {
                    ReportContext = reportContext,
                },
                // Report = reportContext.ToByteString(),
                Signatures = { Hash.Empty.Value, Hash.Empty.Value }
            });
            result.TransactionResult.Error.ShouldContain("Not enough signatures.");
        }
        {
            var result = await RampContractStub.Commit.SendWithExceptionAsync(new CommitInput
            {
                Report =
                {
                    ReportContext = reportContext,
                },
                // Report = reportContext.ToByteString(),
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
                Report =
                {
                    ReportContext = reportContext,
                },
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
                Report =
                {
                    ReportContext = reportContext,
                },
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
                Report =
                {
                    ReportContext = reportContext,
                },
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
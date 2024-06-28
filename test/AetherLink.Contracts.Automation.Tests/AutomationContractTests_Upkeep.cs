using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using AetherLink.Contracts.Oracle;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Oracle;
using Shouldly;
using Xunit;

namespace AetherLink.Contracts.Automation;

public partial class AutomationContractTests
{
    [Fact]
    public async Task RegisterUpkeepTests()
    {
        var (subscriptionId, requestTypeIndex) = await PrepareOracleContractsAsync();

        var registerUpkeepInput = new RegisterUpkeepInput
        {
            Name = "test-upkeep",
            UpkeepContract = UpkeepContractAddress,
            AdminAddress = UserAddress,
            TriggerType = TriggerType.Cron,
            TriggerData = ByteString.Empty,
            PerformData = ByteString.Empty
        };

        var upkeepId = HashHelper.ComputeFrom(registerUpkeepInput);

        var result = await AutomationContractStub.RegisterUpkeep.SendAsync(registerUpkeepInput);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var log = GetLogEvent<RequestStarted>(result.TransactionResult);
        log.RequestId.ShouldBe(upkeepId);
        log.SubscriptionId.ShouldBe(subscriptionId);
        log.RequestingContract.ShouldBe(DefaultAddress);
        log.RequestingInitiator.ShouldBe(DefaultAddress);
        log.RequestTypeIndex.ShouldBe(requestTypeIndex);
        var commitment = Commitment.Parser.ParseFrom(log.Commitment);

        var transmitInput = new TransmitInput
        {
            Report = new Report
            {
                Result = registerUpkeepInput.ToByteString(),
                OnChainMetadata = commitment.ToByteString(),
                Error = ByteString.Empty,
                OffChainMetadata = ByteString.Empty
            }.ToByteString()
        };

        var config = await OracleContractStub.GetConfig.CallAsync(new Empty());
        transmitInput.ReportContext.Add(config.Config.LatestConfigDigest);
        var round = await OracleContractStub.GetLatestRound.CallAsync(new Empty());
        round.Value.ShouldBe(0);

        transmitInput.ReportContext.Add(HashHelper.ComputeFrom(round.Value));
        transmitInput.ReportContext.Add(HashHelper.ComputeFrom(0));
        transmitInput.Signatures.AddRange(new List<ByteString>
        {
            GenerateSignature(Signer1KeyPair.PrivateKey, transmitInput),
            GenerateSignature(Signer2KeyPair.PrivateKey, transmitInput),
            GenerateSignature(Signer3KeyPair.PrivateKey, transmitInput),
            GenerateSignature(Signer4KeyPair.PrivateKey, transmitInput),
            GenerateSignature(Signer5KeyPair.PrivateKey, transmitInput)
        });

        var transmitResult = await OracleContractStub.Transmit.SendAsync(transmitInput);
        transmitResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    [Fact]
    public async Task DeregisterUpkeepTests()
    {
        await PrepareOracleContractsAsync();

        var registerUpkeepInput = new RegisterUpkeepInput
        {
            Name = "test-upkeep",
            UpkeepContract = UpkeepContractAddress,
            AdminAddress = UserAddress,
            TriggerType = TriggerType.Cron,
            TriggerData = ByteString.Empty,
            PerformData = ByteString.Empty
        };

        var upkeepId = HashHelper.ComputeFrom(registerUpkeepInput);
        {
            var result = await AutomationContractUserStub.RegisterUpkeep.SendAsync(registerUpkeepInput);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        {
            var result = await AutomationContractUserStub.DeregisterUpkeep.SendAsync(upkeepId);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<UpkeepRemoved>(result.TransactionResult);
            log.UpkeepId.ShouldBe(upkeepId);
        }
    }

    [Fact]
    public async Task DeregisterUpkeepTests_Fail()
    {
        await PrepareOracleContractsAsync();
        var registerUpkeepInput = new RegisterUpkeepInput
        {
            Name = "test-upkeep",
            UpkeepContract = UpkeepContractAddress,
            AdminAddress = UserAddress,
            TriggerType = TriggerType.Cron,
            TriggerData = ByteString.Empty,
            PerformData = ByteString.Empty
        };

        var upkeepId = HashHelper.ComputeFrom(registerUpkeepInput);
        {
            var result = await AutomationContractUserStub.RegisterUpkeep.SendAsync(registerUpkeepInput);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        {
            var result = await AutomationContractUser2Stub.DeregisterUpkeep.SendWithExceptionAsync(upkeepId);
            result.TransactionResult.Error.ShouldContain("No permission.");
        }

        {
            var result = await AutomationContractUser2Stub.DeleteCommitment.SendWithExceptionAsync(upkeepId);
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
    }

    [Fact]
    public async Task ReportTests_Fail()
    {
        {
            var result = await AutomationContractStub.Report.SendWithExceptionAsync(new ReportInput());
            result.TransactionResult.Error.ShouldContain("Not initialized.");
        }

        var (subscriptionId, requestTypeIndex) = await PrepareOracleContractsAsync();

        {
            var result = await AutomationContractUserStub.Report.SendWithExceptionAsync(new ReportInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }

        var registerUpkeepInput = new RegisterUpkeepInput
        {
            Name = "test-upkeep",
            UpkeepContract = UpkeepContractAddress,
            AdminAddress = UserAddress,
            TriggerType = TriggerType.Cron,
            TriggerData = ByteString.Empty,
            PerformData = ByteString.Empty
        };

        var upkeepId = HashHelper.ComputeFrom(registerUpkeepInput);

        var registerUpkeepResult = await AutomationContractStub.RegisterUpkeep.SendAsync(registerUpkeepInput);
        registerUpkeepResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var log = GetLogEvent<RequestStarted>(registerUpkeepResult.TransactionResult);
        log.RequestId.ShouldBe(upkeepId);
        log.SubscriptionId.ShouldBe(subscriptionId);
        log.RequestingContract.ShouldBe(DefaultAddress);
        log.RequestingInitiator.ShouldBe(DefaultAddress);
        log.RequestTypeIndex.ShouldBe(requestTypeIndex);
        var commitment = Commitment.Parser.ParseFrom(log.Commitment);

        await OracleContractStub.SetConfig.SendAsync(new SetConfigInput
        {
            F = 1,
            Signers = { Accounts[2].Address, Accounts[3].Address, Accounts[4].Address, Accounts[5].Address },
            Transmitters = { DefaultAddress, Accounts[7].Address, Accounts[8].Address, Accounts[9].Address }
        });
        var config = await OracleContractStub.GetLatestConfigDetails.CallAsync(new Empty());
        var round = await OracleContractStub.GetLatestRound.CallAsync(new Empty());

        await AutomationContractStub.SetOracleContractAddress.SendAsync(OracleContractAddress);

        var commitmentWithWrongContent = Commitment.Parser.ParseFrom(log.Commitment);
        commitmentWithWrongContent.SubscriptionId = 2;
        var transmitInput = new TransmitInput
        {
            ReportContext = { config.ConfigDigest, HashHelper.ComputeFrom(round.Value), new Hash() },
            Report = new Report
            {
                Result = Hash.Empty.Value,
                Error = ByteString.Empty,
                OnChainMetadata = commitment.ToByteString(),
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

        await AutomationContractStub.Pause.SendAsync(new Empty());
        {
            var result = await AutomationContractStub.Report.SendWithExceptionAsync(new ReportInput());
            result.TransactionResult.Error.ShouldContain("Contract paused.");
        }
    }
}
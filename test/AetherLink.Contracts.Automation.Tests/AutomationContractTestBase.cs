using System.IO;
using AElf;
using AElf.Boilerplate.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Standards.ACS0;
using AElf.Types;
using AetherLink.Contracts.Automation.Upkeep;
using AetherLink.Contracts.Oracle;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AetherLink.Contracts.Automation;

public class AutomationContractTestBase : DAppContractTestBase<AutomationContractTestModule>
{
    internal Address OracleContractAddress { get; set; }
    internal Address AutomationContractAddress { get; set; }
    internal Address UpkeepContractAddress { get; set; }

    internal AutomationContractContainer.AutomationContractStub AutomationContractStub { get; set; }
    internal AutomationContractContainer.AutomationContractStub AutomationContractUserStub { get; set; }
    internal AutomationContractContainer.AutomationContractStub AutomationContractUser2Stub { get; set; }
    internal ACS0Container.ACS0Stub ZeroContractStub { get; set; }
    internal OracleContractContainer.OracleContractStub OracleContractStub { get; set; }

    protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
    protected Address DefaultAddress => Accounts[0].Address;

    protected ECKeyPair UserKeyPair => Accounts[10].KeyPair;
    protected Address UserAddress => Accounts[10].Address;
    protected ECKeyPair User2KeyPair => Accounts[2].KeyPair;
    protected Address User2Address => Accounts[2].Address;

    protected ECKeyPair Signer1KeyPair => Accounts[1].KeyPair;
    protected Address Signer1Address => Accounts[1].Address;
    protected ECKeyPair Signer2KeyPair => Accounts[2].KeyPair;
    protected Address Signer2Address => Accounts[2].Address;
    protected ECKeyPair Signer3KeyPair => Accounts[3].KeyPair;
    protected Address Signer3Address => Accounts[3].Address;
    protected ECKeyPair Signer4KeyPair => Accounts[4].KeyPair;
    protected Address Signer4Address => Accounts[4].Address;
    protected ECKeyPair Signer5KeyPair => Accounts[5].KeyPair;
    protected Address Signer5Address => Accounts[5].Address;

    protected Address Transmitter1Address => Accounts[6].Address;
    protected Address Transmitter2Address => Accounts[7].Address;
    protected Address Transmitter3Address => Accounts[8].Address;
    protected Address Transmitter4Address => Accounts[9].Address;


    protected AutomationContractTestBase()
    {
        // BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();
        ZeroContractStub = GetContractStub<ACS0Container.ACS0Stub>(BasicContractZeroAddress, DefaultKeyPair);

        // Oracle
        {
            var code = ByteString.CopyFrom(File.ReadAllBytes(typeof(OracleContract).Assembly.Location));
            var contractOperation = new ContractOperation
            {
                ChainId = 9992731,
                CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
                Deployer = DefaultAddress,
                Salt = HashHelper.ComputeFrom("oracle"),
                Version = 1
            };
            contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);

            var result = AsyncHelper.RunSync(async () => await ZeroContractStub.DeploySmartContract.SendAsync(
                new ContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = code,
                    ContractOperation = contractOperation
                }));

            OracleContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
            OracleContractStub =
                GetContractStub<OracleContractContainer.OracleContractStub>(OracleContractAddress, DefaultKeyPair);
        }

        // Automation
        {
            var code = ByteString.CopyFrom(File.ReadAllBytes(typeof(AutomationContract).Assembly.Location));
            var contractOperation = new ContractOperation
            {
                ChainId = 9992731,
                CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
                Deployer = DefaultAddress,
                Salt = HashHelper.ComputeFrom("automation"),
                Version = 1
            };
            contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);

            var result = AsyncHelper.RunSync(async () => await ZeroContractStub.DeploySmartContract.SendAsync(
                new ContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = code,
                    ContractOperation = contractOperation
                }));

            AutomationContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
            AutomationContractStub = GetAutomationContractContainerStub(DefaultKeyPair);
            AutomationContractUserStub = GetAutomationContractContainerStub(UserKeyPair);
            AutomationContractUser2Stub = GetAutomationContractContainerStub(User2KeyPair);
        }

        // Upkeep
        {
            var code = ByteString.CopyFrom(File.ReadAllBytes(typeof(UpkeepContract).Assembly.Location));
            var contractOperation = new ContractOperation
            {
                ChainId = 9992731,
                CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
                Deployer = DefaultAddress,
                Salt = HashHelper.ComputeFrom("upkeep"),
                Version = 1
            };
            contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);

            var result = AsyncHelper.RunSync(async () => await ZeroContractStub.DeploySmartContract.SendAsync(
                new ContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = code,
                    ContractOperation = contractOperation
                }));

            UpkeepContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
            // UpkeepContractStub = GetTester<UpkeepContractContainer.UpkeepContractStub>(AutomationContractAddress,
            //     DefaultKeyPair);
            // AutomationContractUserStub = GetAutomationContractContainerStub(UserKeyPair);
        }
    }

    internal AutomationContractContainer.AutomationContractStub GetAutomationContractContainerStub(ECKeyPair keyPair)
        => GetTester<AutomationContractContainer.AutomationContractStub>(AutomationContractAddress, keyPair);

    internal T GetContractStub<T>(Address contractAddress, ECKeyPair senderKeyPair) where T : ContractStubBase, new()
        => GetTester<T>(contractAddress, senderKeyPair);

    internal ByteString GenerateContractSignature(byte[] privateKey, ContractOperation contractOperation)
    {
        var dataHash = HashHelper.ComputeFrom(contractOperation);
        var signature = CryptoHelper.SignWithPrivateKey(privateKey, dataHash.ToByteArray());
        return ByteStringHelper.FromHexString(signature.ToHex());
    }
}
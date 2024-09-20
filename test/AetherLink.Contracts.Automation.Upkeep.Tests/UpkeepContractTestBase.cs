using System.IO;
using AElf;
using AElf.Boilerplate.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Standards.ACS0;
using AElf.Types;
using AetherLink.Contracts.Oracle;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AetherLink.Contracts.Automation.Upkeep;

public class UpkeepContractTestBase : DAppContractTestBase<UpkeepContractTestModule>
{
    internal Address UpkeepContractAddress { get; set; }
    protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
    protected Address DefaultAddress => Accounts[0].Address;

    internal UpkeepContractContainer.UpkeepContractStub UpkeepContractStub { get; set; }
    internal ACS0Container.ACS0Stub ZeroContractStub { get; set; }


    protected UpkeepContractTestBase()
    {
        ZeroContractStub = GetContractStub<ACS0Container.ACS0Stub>(BasicContractZeroAddress, DefaultKeyPair);

        // Automation
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
            UpkeepContractStub = GetUpkeepContractContainerStub(DefaultKeyPair);
        }
    }

    internal UpkeepContractContainer.UpkeepContractStub GetUpkeepContractContainerStub(ECKeyPair keyPair)
        => GetTester<UpkeepContractContainer.UpkeepContractStub>(UpkeepContractAddress, keyPair);

    internal T GetContractStub<T>(Address contractAddress, ECKeyPair senderKeyPair) where T : ContractStubBase, new()
        => GetTester<T>(contractAddress, senderKeyPair);

    internal ByteString GenerateContractSignature(byte[] privateKey, ContractOperation contractOperation)
    {
        var dataHash = HashHelper.ComputeFrom(contractOperation);
        var signature = CryptoHelper.SignWithPrivateKey(privateKey, dataHash.ToByteArray());
        return ByteStringHelper.FromHexString(signature.ToHex());
    }
}
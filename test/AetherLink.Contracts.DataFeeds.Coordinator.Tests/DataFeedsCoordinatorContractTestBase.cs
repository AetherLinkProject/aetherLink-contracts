using System.IO;
using AElf;
using AElf.Boilerplate.TestBase;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Standards.ACS0;
using AElf.Types;
using AetherLink.Contracts.Oracle;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AetherLink.Contracts.DataFeeds.Coordinator;

public class DataFeedsCoordinatorContractTestBase : DAppContractTestBase<DataFeedsCoordinatorContractTestModule>
{
    internal Address DataFeedsCoordinatorContractAddress { get; set; }
    internal Address OracleContractAddress { get; set; }
    internal ACS0Container.ACS0Stub ZeroContractStub { get; set; }

    internal DataFeedsCoordinatorContractContainer.DataFeedsCoordinatorContractStub CoordinatorContractStub
    {
        get;
        set;
    }

    internal DataFeedsCoordinatorContractContainer.DataFeedsCoordinatorContractStub UserCoordinatorContractStub
    {
        get;
        set;
    }

    internal OracleContractContainer.OracleContractStub OracleContractStub { get; set; }

    protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
    protected Address DefaultAddress => Accounts[0].Address;
    protected ECKeyPair UserKeyPair => Accounts[1].KeyPair;
    protected Address UserAddress => Accounts[1].Address;

    protected readonly IBlockTimeProvider BlockTimeProvider;

    protected DataFeedsCoordinatorContractTestBase()
    {
        BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();

        ZeroContractStub = GetContractStub<ACS0Container.ACS0Stub>(BasicContractZeroAddress, DefaultKeyPair);

        var code = ByteString.CopyFrom(File.ReadAllBytes(typeof(DataFeedsCoordinatorContract).Assembly.Location));
        var contractOperation = new ContractOperation
        {
            ChainId = 9992731,
            CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
            Deployer = DefaultAddress,
            Salt = HashHelper.ComputeFrom("df"),
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

        DataFeedsCoordinatorContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);

        CoordinatorContractStub =
            GetContractStub<DataFeedsCoordinatorContractContainer.DataFeedsCoordinatorContractStub>(
                DataFeedsCoordinatorContractAddress, DefaultKeyPair);
        UserCoordinatorContractStub =
            GetContractStub<DataFeedsCoordinatorContractContainer.DataFeedsCoordinatorContractStub>(
                DataFeedsCoordinatorContractAddress, UserKeyPair);

        code = ByteString.CopyFrom(File.ReadAllBytes(typeof(OracleContract).Assembly.Location));
        contractOperation = new ContractOperation
        {
            ChainId = 9992731,
            CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
            Deployer = DefaultAddress,
            Salt = HashHelper.ComputeFrom("oracle"),
            Version = 1
        };
        contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);

        result = AsyncHelper.RunSync(async () => await ZeroContractStub.DeploySmartContract.SendAsync(
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

    internal T GetContractStub<T>(Address contractAddress, ECKeyPair senderKeyPair) where T : ContractStubBase, new()
    {
        return GetTester<T>(contractAddress, senderKeyPair);
    }

    internal ByteString GenerateContractSignature(byte[] privateKey, ContractOperation contractOperation)
    {
        var dataHash = HashHelper.ComputeFrom(contractOperation);
        var signature = CryptoHelper.SignWithPrivateKey(privateKey, dataHash.ToByteArray());
        return ByteStringHelper.FromHexString(signature.ToHex());
    }
}
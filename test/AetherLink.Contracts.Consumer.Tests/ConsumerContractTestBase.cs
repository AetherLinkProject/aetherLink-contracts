using System.IO;
using AElf;
using AElf.Boilerplate.TestBase;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Standards.ACS0;
using AElf.Types;
using AetherLink.Contracts.DataFeeds.Coordinator;
using AetherLink.Contracts.Oracle;
using AetherLink.Contracts.VRF.Coordinator;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AetherLink.Contracts.Consumer;

public class ConsumerContractTestBase : DAppContractTestBase<ConsumerContractTestModule>
{
    internal Address OracleContractAddress { get; set; }
    internal Address DataFeedsCoordinatorContractAddress { get; set; }
    internal Address VrfCoordinatorContractAddress { get; set; }
    internal Address ConsumerContractAddress { get; set; }

    internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
    internal AEDPoSContractContainer.AEDPoSContractStub ConsensusContractStub { get; set; }
    internal ACS0Container.ACS0Stub ZeroContractStub { get; set; }
    internal OracleContractContainer.OracleContractStub OracleContractStub { get; set; }
    internal DataFeedsCoordinatorContractContainer.DataFeedsCoordinatorContractStub DataFeedsContractStub { get; set; }
    internal VrfCoordinatorContractContainer.VrfCoordinatorContractStub VrfContractStub { get; set; }
    internal ConsumerContractContainer.ConsumerContractStub ConsumerContractStub { get; set; }
    internal ConsumerContractContainer.ConsumerContractStub UserConsumerContractStub { get; set; }

    protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
    protected Address DefaultAddress => Accounts[0].Address;

    protected ECKeyPair UserKeyPair => Accounts[10].KeyPair;
    protected Address UserAddress => Accounts[10].Address;

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

    protected ECKeyPair Transmitter1KeyPair => Accounts[6].KeyPair;
    protected Address Transmitter1Address => Accounts[6].Address;
    protected ECKeyPair Transmitter2KeyPair => Accounts[7].KeyPair;
    protected Address Transmitter2Address => Accounts[7].Address;
    protected ECKeyPair Transmitter3KeyPair => Accounts[8].KeyPair;
    protected Address Transmitter3Address => Accounts[8].Address;
    protected ECKeyPair Transmitter4KeyPair => Accounts[9].KeyPair;
    protected Address Transmitter4Address => Accounts[9].Address;

    protected readonly IBlockTimeProvider BlockTimeProvider;

    protected ConsumerContractTestBase()
    {
        BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();

        ZeroContractStub = GetContractStub<ACS0Container.ACS0Stub>(BasicContractZeroAddress, DefaultKeyPair);
        TokenContractStub =
            GetContractStub<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultKeyPair);
        ConsensusContractStub =
            GetContractStub<AEDPoSContractContainer.AEDPoSContractStub>(ConsensusContractAddress, DefaultKeyPair);

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

        // DataFeeds
        {
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
            DataFeedsContractStub =
                GetContractStub<DataFeedsCoordinatorContractContainer.DataFeedsCoordinatorContractStub>(
                    DataFeedsCoordinatorContractAddress, DefaultKeyPair);
        }

        // Vrf
        {
            var code = ByteString.CopyFrom(File.ReadAllBytes(typeof(VrfCoordinatorContract).Assembly.Location));
            var contractOperation = new ContractOperation
            {
                ChainId = 9992731,
                CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
                Deployer = DefaultAddress,
                Salt = HashHelper.ComputeFrom("vrf"),
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

            VrfCoordinatorContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
            VrfContractStub =
                GetContractStub<VrfCoordinatorContractContainer.VrfCoordinatorContractStub>(
                    VrfCoordinatorContractAddress, DefaultKeyPair);
        }

        // Consumer
        {
            var code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ConsumerContract).Assembly.Location));
            var contractOperation = new ContractOperation
            {
                ChainId = 9992731,
                CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
                Deployer = DefaultAddress,
                Salt = HashHelper.ComputeFrom("consumer"),
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

            ConsumerContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
            ConsumerContractStub =
                GetContractStub<ConsumerContractContainer.ConsumerContractStub>(ConsumerContractAddress,
                    DefaultKeyPair);
            UserConsumerContractStub =
                GetContractStub<ConsumerContractContainer.ConsumerContractStub>(ConsumerContractAddress,
                    UserKeyPair);
        }
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
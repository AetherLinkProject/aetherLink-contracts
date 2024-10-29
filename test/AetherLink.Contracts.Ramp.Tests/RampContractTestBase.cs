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

namespace AetherLink.Contracts.Ramp;

public class RampContractTestBase : DAppContractTestBase<RampContractTestModule>
{
    internal Address RampContractAddress { get; set; }
    internal Address OracleContractAddress { get; set; }
    internal Address TestRampContractAddress { get; set; }
    internal ACS0Container.ACS0Stub ZeroContractStub { get; set; }
    internal RampContractContainer.RampContractStub RampContractStub { get; set; }
    internal RampContractContainer.RampContractStub UserRampContractStub { get; set; }
    internal RampContractContainer.RampContractStub TransmitterRampContractStub { get; set; }
    internal OracleContractContainer.OracleContractStub OracleContractStub { get; set; }
    protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
    protected Address DefaultAddress => Accounts[0].Address;
    protected ECKeyPair UserKeyPair => Accounts[1].KeyPair;
    protected Address UserAddress => Accounts[1].Address;
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

    protected RampContractTestBase()
    {
        BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();

        ZeroContractStub = GetContractStub<ACS0Container.ACS0Stub>(BasicContractZeroAddress, DefaultKeyPair);

        var code = ByteString.CopyFrom(File.ReadAllBytes(typeof(RampContract).Assembly.Location));
        var contractOperation = new ContractOperation
        {
            ChainId = 9992731,
            CodeHash = HashHelper.ComputeFrom(code.ToByteArray()),
            Deployer = DefaultAddress,
            Salt = HashHelper.ComputeFrom("ramp"),
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

        RampContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
        RampContractStub = GetContractStub<RampContractContainer.RampContractStub>(RampContractAddress, DefaultKeyPair);
        UserRampContractStub =
            GetContractStub<RampContractContainer.RampContractStub>(RampContractAddress, UserKeyPair);
        TransmitterRampContractStub =
            GetContractStub<RampContractContainer.RampContractStub>(RampContractAddress, Transmitter1KeyPair);
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

        result = AsyncHelper.RunSync(async () => await ZeroContractStub.DeploySmartContract.SendAsync(
            new ContractDeploymentInput
            {
                Category = KernelConstants.CodeCoverageRunnerCategory,
                Code = ByteString.CopyFrom(
                    File.ReadAllBytes(typeof(TestRampContract.TestRampContract).Assembly.Location))
            }));

        TestRampContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
    }

    internal T GetContractStub<T>(Address contractAddress, ECKeyPair senderKeyPair) where T : ContractStubBase, new()
        => GetTester<T>(contractAddress, senderKeyPair);

    internal ByteString GenerateContractSignature(byte[] privateKey, ContractOperation contractOperation)
        => ByteStringHelper.FromHexString(CryptoHelper
            .SignWithPrivateKey(privateKey, HashHelper.ComputeFrom(contractOperation).ToByteArray()).ToHex());
}
using System.IO;
using AElf;
using AElf.Boilerplate.TestBase;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AetherLink.Contracts.Oracle;

public class OracleContractTestBase : DAppContractTestBase<OracleContractTestModule>
{
    internal Address OracleContractAddress { get; set; }

    internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
    internal ACS0Container.ACS0Stub ZeroContractStub { get; set; }
    internal OracleContractContainer.OracleContractStub OracleContractStub { get; set; }
    internal OracleContractContainer.OracleContractStub UserOracleContractStub { get; set; }
    internal OracleContractContainer.OracleContractStub TransmitterOracleContractStub { get; set; }

    protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
    protected Address DefaultAddress => Accounts[0].Address;
    protected ECKeyPair UserKeyPair => Accounts[11].KeyPair;
    protected Address UserAddress => Accounts[11].Address;

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
    protected ECKeyPair Transmitter5KeyPair => Accounts[10].KeyPair;
    protected Address Transmitter5Address => Accounts[10].Address;

    protected readonly IBlockTimeProvider BlockTimeProvider;

    protected OracleContractTestBase()
    {
        BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();

        ZeroContractStub = GetContractZeroTester(DefaultKeyPair);

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

        TokenContractStub = GetTokenContractStub(DefaultKeyPair);
        OracleContractStub = GetOracleAccountContractStub(DefaultKeyPair);
        UserOracleContractStub = GetOracleAccountContractStub(UserKeyPair);
        TransmitterOracleContractStub = GetOracleAccountContractStub(Transmitter1KeyPair);
    }

    internal OracleContractContainer.OracleContractStub GetOracleAccountContractStub(ECKeyPair senderKeyPair)
    {
        return GetTester<OracleContractContainer.OracleContractStub>(OracleContractAddress, senderKeyPair);
    }

    internal TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair senderKeyPair)
    {
        return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, senderKeyPair);
    }

    internal ACS0Container.ACS0Stub GetContractZeroTester(ECKeyPair senderKeyPair)
    {
        return GetTester<ACS0Container.ACS0Stub>(BasicContractZeroAddress, senderKeyPair);
    }

    internal ByteString GenerateContractSignature(byte[] privateKey, ContractOperation contractOperation)
    {
        var dataHash = HashHelper.ComputeFrom(contractOperation);
        var signature = CryptoHelper.SignWithPrivateKey(privateKey, dataHash.ToByteArray());
        return ByteStringHelper.FromHexString(signature.ToHex());
    }
}
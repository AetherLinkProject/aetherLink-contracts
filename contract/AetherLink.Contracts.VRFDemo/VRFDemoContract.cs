using AElf;
using AElf.Contracts.MultiToken;
using AElf.Contracts.VRFDemo;
using AElf.Sdk.CSharp;
using AElf.Types;
using AetherLink.Contracts.Consumer;
using AetherLink.Contracts.Oracle;
using AetherLink.Contracts.VRF.Coordinator;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.VRFDemo;

public partial class VRFDemoContract : VRFDemoContractContainer.VRFDemoContractBase
{
    private const string
        OracleContractAddress = "21Fh7yog1B741yioZhNAFbs3byJ97jvBmbGAPPZKZpHHog5aEg"; // tDVW oracle contract address

    private const string TokenSymbol = "ELF";
    private const long MinimumPlayAmount = 1_000_000; // 0.01 ELF
    private const long MaximumPlayAmount = 1_000_000_000; // 10 ELF
    private const long SubscriptionId = 1; // input your subscriptionId

    // Initializes the contract
    public override Empty Initialize(Empty input)
    {
        Assert(State.Initialized.Value == false, "Already initialized.");
        State.Initialized.Value = true;
        State.TokenContract.Value = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        State.OracleContract.Value = Address.FromBase58(OracleContractAddress);
        return new Empty();
    }

    public override Empty HandleOracleFulfillment(HandleOracleFulfillmentInput input)
    {
        var userRecord = State.PlayedRecords[input.TraceId];
        if (userRecord != null) return new Empty();
        var randomHashList = HashList.Parser.ParseFrom(input.Response);
        var userAddress = userRecord.UserAddress;
        var playAmount = userRecord.PlayAmount;
        if (IsWinner(randomHashList.Data[0]))
        {
            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = userAddress,
                Symbol = TokenSymbol,
                Amount = playAmount
            });

            Context.Fire(new PlayOutcomeEvent
            {
                Won = playAmount
            });
        }
        else
        {
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = userAddress,
                To = Context.Self,
                Symbol = TokenSymbol,
                Amount = playAmount
            });

            Context.Fire(new PlayOutcomeEvent
            {
                Won = -playAmount
            });
        }

        return new Empty();
    }

    public override Empty Play(Int64Value input)
    {
        var playAmount = input.Value;
        Assert(playAmount is >= MinimumPlayAmount and <= MaximumPlayAmount, "Invalid play amount.");
        var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
        {
            Owner = Context.Sender,
            Symbol = TokenSymbol
        }).Balance;
        Assert(balance >= playAmount, "Insufficient balance.");

        var contractBalance = State.TokenContract.GetBalance.Call(new GetBalanceInput
        {
            Owner = Context.Self,
            Symbol = TokenSymbol
        }).Balance;
        Assert(contractBalance >= playAmount, "Insufficient contract balance.");

        var keyHashs = State.OracleContract.GetProvingKeyHashes.Call(new Empty());
        var keyHash = keyHashs.Data[0];
        var specificData = new SpecificData
        {
            KeyHash = keyHash,
            NumWords = 1,
            RequestConfirmations = 1
        }.ToByteString();

        var request = new SendRequestInput
        {
            SubscriptionId = SubscriptionId,
            RequestTypeIndex = 2,
            SpecificData = specificData,
        };

        var traceId = HashHelper.ConcatAndCompute(
            HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(Context.CurrentBlockTime),
                HashHelper.ComputeFrom(Context.Origin)), HashHelper.ComputeFrom(request));
        request.TraceId = traceId;
        State.OracleContract.SendRequest.Send(request);

        State.PlayedRecords[traceId] = new()
        {
            UserAddress = Context.Sender,
            PlayAmount = input.Value
        };

        return new Empty();
    }

    private bool IsWinner(Hash randomHash)
        => int.Parse(randomHash.ToHex().Substring(0, 8), System.Globalization.NumberStyles.HexNumber) % 2 == 0;
}
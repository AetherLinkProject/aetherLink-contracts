using System;
using System.Globalization;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Randoms;
using AElf.Sdk.CSharp;
using AElf.Types;
using AetherLink.Contracts.Consumer;
using AetherLink.Contracts.Oracle;
using AetherLink.Contracts.VRF.Coordinator;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.Randoms;

public class RandomsContract : RandomsContractContainer.RandomsContractBase
{
    private const string
        OracleContractAddress = "21Fh7yog1B741yioZhNAFbs3byJ97jvBmbGAPPZKZpHHog5aEg"; // tDVW oracle contract address

    private const long SubscriptionId = 8;

    public override Empty Initialize(Empty input)
    {
        Assert(State.Initialized.Value == false, "Already initialized.");
        State.Initialized.Value = true;
        State.OracleContract.Value = Address.FromBase58(OracleContractAddress);
        return new Empty();
    }

    public override Empty HandleOracleFulfillment(HandleOracleFulfillmentInput input)
    {
        var randomHashList = HashList.Parser.ParseFrom(input.Response);
        var result = randomHashList.Data[0];

        State.RandomResults[input.TraceId] = new RandomRequestInfo { Number = RollDice(result) };
        return new Empty();
    }

    public override Empty Play(Empty input)
    {
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

        State.RandomResults[traceId] = new();

        Context.Fire(new RandomPlayed
        {
            TraceId = traceId
        });

        return new Empty();
    }

    public override RandomRequestInfo GetPlayedResult(Hash traceId) => State.RandomResults[traceId];

    private int RollDice(Hash randomHash)
        => Math.Abs(int.Parse(randomHash.ToHex().Substring(0, 8), NumberStyles.HexNumber)) % 6 + 1;
}
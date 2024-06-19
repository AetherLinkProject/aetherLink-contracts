using AElf.CSharp.Core;
using AElf.Types;
using AetherLink.Contracts.Consumer;
using AetherLink.Contracts.Oracle;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.GoogleAuthDemo;

public partial class GoogleAuthDemoContract : GoogleAuthDemoContractContainer.GoogleAuthDemoContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        State.OracleContract.Value = input.Oracle;
        State.Initialized.Value = true;

        return new Empty();
    }

    public override Empty StartOracleRequest(StartOracleRequestInput input)
    {
        State.OracleContract.SendRequest.Send(new SendRequestInput
        {
            SubscriptionId = input.SubscriptionId,
            RequestTypeIndex = input.RequestTypeIndex,
            SpecificData = input.SpecificData
        });

        return new Empty();
    }

    public override Empty HandleOracleFulfillment(HandleOracleFulfillmentInput input)
    {
        var round = State.LatestRound.Value.Add(1);

        State.FulfillData[input.RequestId] = new GoogleRoundData
        {
            Data = input.Response,
            RoundId = round,
            UpdatedAt = Context.CurrentBlockTime
        };

        return new Empty();
    }

    public override GoogleRoundData GetFulfillResponse(Hash input)
    {
        return State.FulfillData[input];
    }
}
using AElf;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.AIFeeds.Demo;

public partial class AIFeedsDemoContract : AIFeedsDemoContractContainer.AIFeedsDemoContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        Assert(input != null, "Invalid input.");
        Assert(input.Admin == null || !input.Admin.Value.IsNullOrEmpty(), "Invalid input admin.");
        State.Admin.Value = input.Admin ?? Context.Sender;
        State.AIFeedsContract.Value = input.AiOracle;
        State.Initialized.Value = true;

        return new Empty();
    }

    public override Empty StartAIRequest(StartAIRequestInput input)
    {
        var info = new AIRequestInfo { Name = input.Name };
        var traceId = HashHelper.ComputeFrom(info);
        State.AIRequestInfoMap[traceId] = info;
        State.AIFeedsContract.StartAIRequest.Send(new()
        {
            Model = ModelType.ChatGpt,
            Admin = Context.Sender,
            FulfillAddress = Context.Self,
            TraceId = traceId,
            Description = Description.Parser.ParseFrom(input.Description)
        });
        return new Empty();
    }

    public override ChatGptResult GetAIResult(Hash traceId) => State.AIRequestInfoMap[traceId].Result;

    public override Empty HandleAIFeedsFulfillment(HandleAIFeedsFulfillmentInput input)
    {
        State.AIRequestInfoMap[input.TraceId].Result = ChatGptResult.Parser.ParseFrom(input.Response);
        return new Empty();
    }
}
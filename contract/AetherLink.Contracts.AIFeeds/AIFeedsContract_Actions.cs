using AElf;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.AIFeeds;

public partial class AIFeedsContract
{
    public override Empty StartAIRequest(StartAIRequestInput input)
    {
        Assert(IsHashValid(input.TraceId), "Invalid trace id.");
        Assert(IsAddressValid(input.Admin), "Invalid admin address.");
        Assert(IsAddressValid(input.FulfillAddress), "Invalid fulfill contract address.");
        Assert(input.Description != null, "Invalid description.");

        var request = new AIRequest
        {
            Model = input.Model,
            Admin = input.Admin,
            FulfillAddress = input.FulfillAddress,
            TraceId = input.TraceId,
            Description = input.Description,
        };
        var requestId = HashHelper.ComputeFrom(request);
        request.Status = AIRequestStatusType.Started;
        State.AIRequestInfoMap[requestId] = request;

        Context.Fire(new RequestStarted
        {
            RequestId = requestId,
            Commitment = request.Description.ToByteString()
        });

        return new Empty();
    }

    public override AIRequest GetAIRequest(Hash id)
    {
        return State.AIRequestInfoMap[id];
    }

    public override Empty AIRequestTransmit(AIRequestTransmitInput input)
    {
        var publicKey = Context.RecoverPublicKey(input.Signature.ToByteArray(),
            HashHelper.ConcatAndCompute(
                HashHelper.ComputeFrom(input.Report.ToByteArray()),
                HashHelper.ComputeFrom(input.OracleContext.ToString())).ToByteArray());

        Assert(publicKey != null, "Invalid signature.");
        var enclaveAddress = Address.FromPublicKey(publicKey);
        Assert(State.Config.Value.Enclaves.Contains(enclaveAddress), "Unauthorized enclave.");

        Assert(input.OracleContext != null && IsHashValid(input.OracleContext.RequestId), "Invalid Oracle context.");
        var request = State.AIRequestInfoMap[input.OracleContext.RequestId];
        Assert(request != null, $"Not exist ai request {input.OracleContext.RequestId}.");

        Context.SendInline(request.FulfillAddress,
            nameof(AIRequestInterfaceContainer.AIRequestInterfaceReferenceState.HandleAIFeedsFulfillment),
            new HandleAIFeedsFulfillmentInput { TraceId = request.TraceId, Response = input.Report });

        request.Status = AIRequestStatusType.Finished;
        State.AIRequestInfoMap[input.OracleContext.RequestId] = request;

        Context.Fire(new AIReportTransmitted
        {
            RequestId = input.OracleContext.RequestId,
            Transmitter = Context.Sender,
            Enclave = enclaveAddress
        });

        return new Empty();
    }
}
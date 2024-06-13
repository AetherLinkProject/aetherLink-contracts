using AElf;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AetherLink.Contracts.Consumer;
using Coordinator;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Oracle;

namespace AetherLink.Contracts.Oracle;

public partial class OracleContract
{
    public override Empty SendRequest(SendRequestInput input)
    {
        CheckInitialized();
        CheckUnpause();
        ValidateSendRequestInput(input);

        var subscription = State.Subscriptions[input.SubscriptionId];
        Assert(subscription != null, "Subscription not found.");

        var consumer = State.Consumers[Context.Sender][input.SubscriptionId];
        Assert(consumer != null, "Consumer not found in subscription.");

        consumer.InitiatedRequests = consumer.InitiatedRequests.Add(1);

        var coordinator = State.Coordinators[input.RequestTypeIndex];

        Assert(coordinator != null, "Coordinator not found.");
        Assert(coordinator.Status, "Coordinator not available.");

        Context.SendInline(coordinator.CoordinatorContractAddress,
            nameof(CoordinatorInterfaceContainer.CoordinatorInterfaceReferenceState.SendRequest), new Request
            {
                RequestingContract = Context.Sender,
                SubscriptionId = input.SubscriptionId,
                SubscriptionOwner = subscription.Owner,
                InitiatedRequests = consumer.InitiatedRequests,
                CompletedRequests = consumer.CompletedRequests,
                SpecificData = input.SpecificData
            }.ToByteString());

        Context.Fire(new OracleRequestSent
        {
            RequestingContract = Context.Sender,
            RequestInitiator = Context.Origin,
            SubscriptionId = input.SubscriptionId,
            SubscriptionOwner = subscription.Owner,
            SpecificData = input.SpecificData
        });

        return new Empty();
    }

    public override Empty StartRequest(StartRequestInput input)
    {
        CheckInitialized();
        CheckCoordinatorContractPermission(input.RequestTypeIndex);
        ValidateStartRequestInput(input);

        var requestId = input.RequestId;
        Assert(State.RequestStartedAdminMap[requestId] == null, $"Request {requestId} is started.");
        State.RequestStartedAdminMap[requestId] = Context.Origin;

        Context.Fire(new RequestStarted
        {
            RequestId = requestId,
            RequestingContract = input.RequestingContract,
            RequestingInitiator = Context.Origin,
            SubscriptionId = input.SubscriptionId,
            SubscriptionOwner = input.SubscriptionOwner,
            Commitment = input.Commitment,
            RequestTypeIndex = input.RequestTypeIndex
        });

        return new Empty();
    }

    public override Empty Fulfill(FulfillInput input)
    {
        CheckInitialized();
        CheckUnpause();
        ValidateFulfillInput(input);
        ValidateCommitment(input.Commitment);

        var commitment = input.Commitment;
        Assert(Context.Sender == commitment.Coordinator, "Commitment mismatches.");

        Assert(State.Subscriptions[commitment.SubscriptionId] != null, "Subscription not found.");

        var consumer = State.Consumers[commitment.Client][commitment.SubscriptionId];

        Assert(consumer != null, "Consumer not found in subscription.");

        consumer.CompletedRequests = consumer.CompletedRequests.Add(1);

        var coordinator = State.Coordinators[commitment.RequestTypeIndex];

        Assert(coordinator != null && coordinator.Status, "Coordinator not available.");

        Context.SendInline(commitment.Client,
            nameof(RequestInterfaceContainer.RequestInterfaceReferenceState.HandleOracleFulfillment),
            new HandleOracleFulfillmentInput
            {
                RequestId = commitment.RequestId,
                Response = input.Response,
                Err = input.Err,
                RequestTypeIndex = coordinator.RequestTypeIndex
            });

        Context.Fire(new RequestProcessed
        {
            SubscriptionId = commitment.SubscriptionId,
            RequestId = commitment.RequestId,
            Response = input.Response,
            Transmitter = input.Transmitter,
            Err = input.Err
        });

        return new Empty();
    }

    private void ValidateFulfillInput(FulfillInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(IsAddressValid(input.Transmitter), "Invalid transmitter.");
        Assert(!input.Response.IsNullOrEmpty() || !input.Err.IsNullOrEmpty(), "Invalid response or err.");
    }

    private void ValidateReport(Report report)
    {
        Assert(report != null, "Invalid report.");
        Assert(!report.Result.IsNullOrEmpty() || !report.Error.IsNullOrEmpty(), "Invalid report response or err.");
        Assert(!report.OnChainMetadata.IsNullOrEmpty(), "Invalid report on chain metadata.");
    }

    private void ValidateCommitment(Commitment commitment)
    {
        Assert(commitment != null, "Invalid commitment.");
        Assert(IsHashValid(commitment.RequestId), "Invalid commitment request id.");
        Assert(IsAddressValid(commitment.Coordinator), "Invalid commitment coordinator.");
        Assert(IsAddressValid(commitment.Client), "Invalid commitment client.");
        Assert(commitment.SubscriptionId > 0, "Invalid commitment subscription id.");
        Assert(commitment.TimeoutTimestamp != null, "Invalid commitment timeout timestamp.");
        Assert(commitment.RequestTypeIndex > 0, "Invalid commitment request type index.");
    }

    public override Empty Transmit(TransmitInput input)
    {
        CheckInitialized();
        CheckTransmitterPermission();
        ValidateTransmitInput(input);

        var configDigest = input.ReportContext[0];
        Assert(State.Config.Value.LatestConfigDigest == configDigest, "Config digest mismatch.");

        // TODO check round number
        // var hashOfRound = input.ReportContext[1];
        // Assert(HashHelper.ComputeFrom(State.LatestRound.Value) == hashOfRound, "Round number mismatch.");
        State.LatestRound.Value = State.LatestRound.Value.Add(1);

        var report = Report.Parser.ParseFrom(input.Report);
        ValidateReport(report);

        var commitment = Commitment.Parser.ParseFrom(report.OnChainMetadata);
        ValidateCommitment(commitment);

        Context.SendInline(commitment.Coordinator,
            nameof(CoordinatorInterfaceContainer.CoordinatorInterfaceReferenceState.Report), new ReportInput
            {
                Transmitter = Context.Sender,
                ReportContext = { input.ReportContext },
                Report = input.Report,
                Signatures = { input.Signatures }
            });

        Context.Fire(new Transmitted
        {
            ConfigDigest = configDigest,
            EpochAndRound = State.LatestRound.Value,
            RequestId = commitment.RequestId,
            Transmitter = Context.Sender
        });

        return new Empty();
    }

    private void ValidateTransmitInput(TransmitInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(input.ReportContext != null && input.ReportContext.Count == OracleContractConstants.ReportContextSize,
            "Invalid input report context.");
        Assert(IsHashValid(input.ReportContext[0]), "Invalid input config digest.");
        Assert(IsHashValid(input.ReportContext[1]), "Invalid input epochAndRound.");
        Assert(!input.Report.IsNullOrEmpty(), "Invalid input report.");
        Assert(input.Signatures != null && input.Signatures.Count > 0, "Invalid input signature.");
    }

    private void ValidateSendRequestInput(SendRequestInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(input.SubscriptionId > 0, "Invalid subscription id.");
        Assert(input.RequestTypeIndex > 0, "Invalid request type index.");
    }

    public override Empty CancelRequest(CancelRequestInput input)
    {
        CheckInitialized();
        CheckUnpause();
        ValidateCancelRequestInput(input);

        var subscription = State.Subscriptions[input.SubscriptionId];
        Assert(subscription != null, "Subscription not found.");

        var consumer = State.Consumers[input.Consumer][input.SubscriptionId];
        Assert(consumer != null, "Consumer not found in subscription.");

        var coordinator = State.Coordinators[input.RequestTypeIndex];
        Assert(coordinator != null, "Coordinator not found.");
        Assert(coordinator.Status, "Coordinator not available.");

        var requestAdmin = State.RequestStartedAdminMap[input.RequestId];
        Assert(requestAdmin != null && requestAdmin == Context.Origin, "No permission.");

        Context.SendInline(coordinator.CoordinatorContractAddress,
            nameof(CoordinatorInterfaceContainer.CoordinatorInterfaceReferenceState.DeleteCommitment), input.RequestId);

        consumer.CompletedRequests = consumer.CompletedRequests.Add(1);

        Context.Fire(new RequestCancelled { RequestId = input.RequestId });

        return new Empty();
    }

    private void ValidateCancelRequestInput(CancelRequestInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(IsHashValid(input.RequestId), "Invalid input request id.");
        Assert(input.SubscriptionId > 0, "Invalid input subscription id.");
        Assert(IsAddressValid(input.Consumer), "Invalid input consumer.");
        Assert(input.RequestTypeIndex > 0, "Invalid input request type index.");
    }
}
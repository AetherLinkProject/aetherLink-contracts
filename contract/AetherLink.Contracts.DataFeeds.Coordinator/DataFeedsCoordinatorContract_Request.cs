using System.Collections.Generic;
using AElf;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using AetherLink.Contracts.Oracle;
using Coordinator;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Oracle;

namespace AetherLink.Contracts.DataFeeds.Coordinator;

public partial class DataFeedsCoordinatorContract
{
    public override Empty SendRequest(Request input)
    {
        CheckInitialized();
        CheckUnpause();
        CheckOracleContractPermission();
        ValidateRequest(input);

        var commitment = StartRequest(input, State.OracleContract.Value);

        Context.Fire(new RequestSent
        {
            RequestId = commitment.RequestId,
            Commitment = commitment.ToByteString(),
            RequestingContract = input.RequestingContract,
            RequestingInitiator = Context.Origin
        });

        return new Empty();
    }

    public override Empty Report(ReportInput input)
    {
        CheckInitialized();
        CheckUnpause();
        CheckOracleContractPermission();

        ValidateReportInput(input);

        var report = global::Oracle.Report.Parser.ParseFrom(input.Report);
        ValidateReport(report);

        var commitment = Commitment.Parser.ParseFrom(report.OnChainMetadata);
        Assert(IsHashValid(commitment.RequestId), "Invalid commitment request id.");

        Assert(State.RequestCommitmentMap[commitment.RequestId] != null, "Invalid request id.");

        var commitmentHash = HashHelper.ComputeFrom(commitment);

        Assert(State.RequestCommitmentMap[commitment.RequestId] == commitmentHash, "Invalid commitment.");

        VerifyThresholdSignature(input);

        State.OracleContract.Fulfill.Send(new FulfillInput
        {
            Response = report.Result,
            Err = report.Error,
            Transmitter = input.Transmitter,
            Commitment = commitment
        });

        // TODO pay the bill

        Context.Fire(new Reported
        {
            RequestId = commitment.RequestId,
            Transmitter = input.Transmitter
        });

        return new Empty();
    }

    private void ValidateReportInput(ReportInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(IsAddressValid(input.Transmitter), "Invalid input transmitter.");
        Assert(
            input.ReportContext != null &&
            input.ReportContext.Count == DataFeedsCoordinatorContractConstants.ReportContextSize,
            "Invalid input report context.");
        Assert(IsHashValid(input.ReportContext[0]), "Invalid input config digest.");
        Assert(IsHashValid(input.ReportContext[1]), "Invalid input epochAndRound.");
        Assert(!input.Report.IsNullOrEmpty(), "Invalid input report.");
        Assert(input.Signatures != null && input.Signatures.Count > 0, "Invalid input signature.");
    }

    private void ValidateRequest(Request input)
    {
        Assert(input != null, "Invalid input.");
        Assert(IsAddressValid(input.RequestingContract), "Invalid input requesting contract.");
        Assert(input.SubscriptionId > 0, "Invalid input subscription id.");
        Assert(input.InitiatedRequests >= 0, "Invalid input initiated requests.");
        Assert(input.CompletedRequests >= 0, "Invalid input completed requests.");
        Assert(IsAddressValid(input.SubscriptionOwner), "Invalid input subscription owner.");
    }

    private Commitment StartRequest(Request request, Address oracle)
    {
        var timeoutTimestamp = Context.CurrentBlockTime.AddSeconds(State.Config.Value.RequestTimeoutSeconds);

        var requestId = HashHelper.ComputeFrom(new RequestInfo
        {
            Coordinator = Context.Self,
            RequestingContract = request.RequestingContract,
            SubscriptionId = request.SubscriptionId,
            Nonce = request.InitiatedRequests,
            TimeoutTimestamp = timeoutTimestamp,
            TraceId = request.TraceId,
            RequestInitiator = Context.Origin
        });

        var commitment = new Commitment
        {
            Coordinator = Context.Self,
            Client = request.RequestingContract,
            SubscriptionId = request.SubscriptionId,
            TimeoutTimestamp = timeoutTimestamp,
            RequestId = requestId,
            SpecificData = request.SpecificData,
            TraceId = request.TraceId,
            RequestTypeIndex = State.RequestTypeIndex.Value
        };

        State.RequestCommitmentMap[requestId] = HashHelper.ComputeFrom(commitment);

        State.OracleContract.StartRequest.Send(new StartRequestInput
        {
            RequestId = commitment.RequestId,
            RequestingContract = request.RequestingContract,
            SubscriptionId = request.SubscriptionId,
            SubscriptionOwner = request.SubscriptionOwner,
            Commitment = commitment.ToByteString(),
            RequestTypeIndex = State.RequestTypeIndex.Value
        });

        return commitment;
    }

    private void ValidateReport(Report report)
    {
        Assert(!report.Result.IsNullOrEmpty() || !report.Error.IsNullOrEmpty(), "Invalid report response or err.");
        Assert(!report.OnChainMetadata.IsNullOrEmpty(), "Invalid report on chain metadata.");
    }

    private void VerifyThresholdSignature(ReportInput input)
    {
        var getConfigOutput = State.OracleContract.GetConfig.Call(new Empty());

        var expectedNumSignatures = (getConfigOutput.Config.N + getConfigOutput.Config.F) / 2 + 1;
        Assert(input.Signatures.Count >= expectedNumSignatures, "Not enough signatures.");

        var signed = new HashSet<Address>();

        var hash = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(input.Report.ToByteArray()),
            HashHelper.ComputeFrom(input.ReportContext.ToString()));
        foreach (var signature in input.Signatures)
        {
            var publicKey = Context.RecoverPublicKey(signature.ToByteArray(), hash.ToByteArray());

            Assert(publicKey != null, "Invalid signature.");

            var address = Address.FromPublicKey(publicKey);

            Assert(getConfigOutput.Signers.Contains(address), "Unauthorized signer.");

            Assert(!signed.Contains(address), "Duplicate signature.");

            signed.Add(address);
        }
    }

    public override Empty DeleteCommitment(Hash input)
    {
        CheckInitialized();
        CheckUnpause();
        CheckOracleContractPermission();

        var commitmentHash = State.RequestCommitmentMap[input];
        Assert(commitmentHash != null, "Request id not found.");

        State.RequestCommitmentMap.Remove(input);

        Context.Fire(new CommitmentDeleted
        {
            RequestId = input
        });

        return new Empty();
    }
}
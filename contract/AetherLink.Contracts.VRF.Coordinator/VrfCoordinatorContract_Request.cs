using System.Linq;
using AElf;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using AetherLink.Contracts.Oracle;
using Coordinator;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Oracle;

namespace AetherLink.Contracts.VRF.Coordinator;

public partial class VrfCoordinatorContract
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

        var response = GetRandomNumberFromVrfProof(input, report, commitment);

        State.RequestCommitmentMap.Remove(commitment.RequestId);

        State.OracleContract.Fulfill.Send(new FulfillInput
        {
            Response = response.ToByteString(),
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
            input.ReportContext.Count == VrfCoordinatorContractConstants.ReportContextSize,
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
        var specificData = SpecificData.Parser.ParseFrom(request.SpecificData);
        ValidateSpecificData(specificData);
        var keyHashes = State.OracleContract.GetProvingKeyHashes.Call(new Empty());
        Assert(keyHashes.Data.Contains(specificData.KeyHash), "Invalid key hash.");

        var timeoutTimestamp = Context.CurrentBlockTime.AddSeconds(State.Config.Value.RequestTimeoutSeconds);

        var currentNonce = request.InitiatedRequests;

        var requestId = HashHelper.ComputeFrom(new RequestInfo
        {
            Coordinator = Context.Self,
            RequestingContract = request.RequestingContract,
            SubscriptionId = request.SubscriptionId,
            Nonce = currentNonce,
            TimeoutTimestamp = timeoutTimestamp,
            RequestInitiator = Context.Origin
        });

        var preSeed = HashHelper.ConcatAndCompute(HashHelper.ConcatAndCompute(specificData.KeyHash,
            HashHelper.ComputeFrom(request.RequestingContract),
            HashHelper.ComputeFrom(request.SubscriptionId)), HashHelper.ComputeFrom(currentNonce));
        specificData.PreSeed = preSeed;
        specificData.BlockNumber = Context.CurrentHeight;

        var commitment = new Commitment
        {
            Coordinator = Context.Self,
            Client = request.RequestingContract,
            SubscriptionId = request.SubscriptionId,
            TimeoutTimestamp = timeoutTimestamp,
            RequestId = requestId,
            SpecificData = specificData.ToByteString(),
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
            RequestTypeIndex = State.RequestTypeIndex.Value,
        });

        return commitment;
    }

    private void ValidateSpecificData(SpecificData specificData)
    {
        var config = State.Config.Value;
        Assert(specificData.NumWords > 0 && specificData.NumWords <= config.MaxNumWords,
            "Invalid extra data num words.");
        Assert(specificData.RequestConfirmations >= config.MinimumRequestConfirmations &&
               specificData.RequestConfirmations <=
               config.MaxRequestConfirmations, "Invalid extra data request confirmations.");
        Assert(IsHashValid(specificData.KeyHash), "Invalid extra data key hash.");
    }

    private void ValidateReport(Report report)
    {
        Assert(!report.Result.IsNullOrEmpty() || !report.Error.IsNullOrEmpty(), "Invalid report response or err.");
        Assert(!report.OnChainMetadata.IsNullOrEmpty(), "Invalid report on chain metadata.");
    }

    private HashList GetRandomNumberFromVrfProof(ReportInput input, Report report, Commitment commitment)
    {
        var response = new HashList();

        var specificData = SpecificData.Parser.ParseFrom(commitment.SpecificData);

        var hash = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(input.Report.ToByteArray()),
            HashHelper.ComputeFrom(input.ReportContext.ToString()));
        var publicKey = Context.RecoverPublicKey(input.Signatures.First().ToByteArray(), hash.ToByteArray());
        Assert(publicKey != null, "Invalid signature.");
        Assert(
            HashHelper.ComputeFrom(publicKey.ToHex()) == specificData.KeyHash, "Invalid public key.");

        Assert(Context.CurrentHeight >= specificData.BlockNumber + specificData.RequestConfirmations,
            "Not wait enough confirmations.");

        if (report.Result.IsNullOrEmpty()) return response;

        var random = State.ConsensusContract.GetRandomHash.Call(new Int64Value
        {
            Value = specificData.BlockNumber
        });

        var alpha = HashHelper.ConcatAndCompute(random, specificData.PreSeed);

        Context.ECVrfVerify(publicKey, alpha.ToByteArray(), report.Result.ToByteArray(), out var beta);
        Assert(beta != null && beta.Length > 0, "Vrf verification fail.");

        var randomHash = Hash.LoadFromByteArray(beta);

        for (var i = 0; i < specificData.NumWords; i++)
        {
            response.Data.Add(HashHelper.ConcatAndCompute(randomHash, HashHelper.ComputeFrom(i)));
        }

        return response;
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
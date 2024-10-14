using System.Collections.Generic;
using AElf;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using AetherLink.Contracts.Oracle;
using AetherLink.Contracts.Upkeep;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Oracle;

namespace AetherLink.Contracts.Automation;

public partial class AutomationContract
{
    public override Empty RegisterUpkeep(RegisterUpkeepInput input)
    {
        CheckInitialized();
        CheckUnpause();
        Assert(IsAddressValid(input.AdminAddress), "Invalid Admin Address.");
        Assert(IsAddressValid(input.UpkeepContract), "Invalid Upkeep Contract Address.");

        var upkeepId = HashHelper.ComputeFrom(input);
        Assert(State.RegisteredUpkeepMap[upkeepId] == null, $"Upkeep {upkeepId} is registered.");
        State.RegisteredUpkeepMap[upkeepId] = new UpkeepInfo
        {
            Name = input.Name,
            TriggerType = input.TriggerType,
            AdminAddress = input.AdminAddress,
            UpkeepContract = input.UpkeepContract
        };

        State.OracleContract.StartRequest.Send(new StartRequestInput
        {
            RequestId = upkeepId,
            SubscriptionOwner = Context.Sender,
            RequestingContract = Context.Sender,
            RequestTypeIndex = State.RequestTypeIndex.Value,
            SubscriptionId = State.SubscriptionId.Value,
            Commitment = new Commitment
            {
                RequestId = upkeepId,
                Client = Context.Sender,
                // Coordinator Address is for Oracle Contract fulfill report
                Coordinator = Context.Self,
                SpecificData = input.ToByteString(),
                RequestTypeIndex = State.RequestTypeIndex.Value,
                SubscriptionId = State.SubscriptionId.Value,
                TimeoutTimestamp = Context.CurrentBlockTime.AddSeconds(State.Config.Value.RequestTimeoutSeconds)
            }.ToByteString()
        });

        Context.Fire(new UpkeepRegistered
        {
            UpkeepId = upkeepId,
            Name = input.Name,
            TriggerType = input.TriggerType,
            UpkeepContract = input.UpkeepContract
        });

        return new Empty();
    }

    public override Empty DeregisterUpkeep(Hash upkeepId)
    {
        CheckInitialized();

        var registeredUpkeep = State.RegisteredUpkeepMap[upkeepId];
        Assert(registeredUpkeep != null, "Request id not found.");
        CheckUpkeepAdminPermission(upkeepId);

        State.OracleContract.CancelRequest.Send(new CancelRequestInput
        {
            RequestId = upkeepId,
            SubscriptionId = State.SubscriptionId.Value,
            Consumer = Context.Self,
            RequestTypeIndex = State.RequestTypeIndex.Value
        });

        return new Empty();
    }

    public override UpkeepInfo GetUpkeepInfo(Hash upkeepId)
    {
        Assert(State.RegisteredUpkeepMap[upkeepId] != null, $"Upkeep {upkeepId} not exist.");
        return State.RegisteredUpkeepMap[upkeepId];
    }

    public override Empty DeleteCommitment(Hash upkeepId)
    {
        CheckInitialized();
        CheckUnpause();
        CheckOracleContractPermission();

        var registeredUpkeep = State.RegisteredUpkeepMap[upkeepId];
        Assert(registeredUpkeep != null, "Request id not found.");

        State.RegisteredUpkeepMap.Remove(upkeepId);

        Context.Fire(new UpkeepRemoved { UpkeepId = upkeepId });

        return new Empty();
    }

    public override Empty Report(ReportInput input)
    {
        Assert(input != null, "Invalid input.");

        CheckInitialized();
        CheckUnpause();
        CheckOracleContractPermission();

        Assert(!input.Report.IsNullOrEmpty(), "Invalid input report.");
        var report = global::Oracle.Report.Parser.ParseFrom(input.Report);

        Assert(!report.OnChainMetadata.IsNullOrEmpty(), "Invalid report on chain metadata.");
        var meta = Commitment.Parser.ParseFrom(report.OnChainMetadata);
        Assert(IsHashValid(meta.RequestId), "Invalid commitment request id.");

        var upkeepId = meta.RequestId;
        Assert(State.RegisteredUpkeepMap[upkeepId] != null, "Not exist upkeep.");
        VerifyThresholdSignature(input);

        var originData = RegisterUpkeepInput.Parser.ParseFrom(meta.SpecificData);
        Context.SendInline(State.RegisteredUpkeepMap[upkeepId].UpkeepContract,
            nameof(UpkeepInterfaceContainer.UpkeepInterfaceReferenceState.PerformUpkeep),
            new PerformUpkeepInput { PerformData = originData.PerformData, ReportResult = report.Result });

        Context.Fire(new UpkeepPerformed { UpkeepId = upkeepId });

        return new Empty();
    }

    private void VerifyThresholdSignature(ReportInput input)
    {
        Assert(input.Signatures != null && input.Signatures.Count > 0, "Invalid input signature.");
        var oracleConfig = State.OracleContract.GetConfig.Call(new Empty());
        var expectedNumSignatures = (oracleConfig.Config.N + oracleConfig.Config.F) / 2 + 1;
        Assert(input.Signatures.Count >= expectedNumSignatures, "Not enough signatures.");

        var signed = new HashSet<Address>();
        var hash = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(input.Report.ToByteArray()),
            HashHelper.ComputeFrom(input.ReportContext.ToString()));

        foreach (var signature in input.Signatures)
        {
            var publicKey = Context.RecoverPublicKey(signature.ToByteArray(), hash.ToByteArray());
            Assert(publicKey != null, "Invalid signature.");

            var address = Address.FromPublicKey(publicKey);
            Assert(oracleConfig.Signers.Contains(address), "Unauthorized signer.");
            Assert(!signed.Contains(address), "Duplicate signature.");

            signed.Add(address);
        }
    }
}
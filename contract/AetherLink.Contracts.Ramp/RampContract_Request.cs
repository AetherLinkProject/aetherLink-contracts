using System.Collections.Generic;
using AElf;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Ramp;

namespace AetherLink.Contracts.Ramp;

public partial class RampContract
{
    public override Empty Send(SendInput input)
    {
        CheckInitialized();

        Assert(input != null, "Invalid send input.");
        Assert(State.RampSenders[Context.Sender] != null, "Invalid sender.");
        Assert(State.Config.Value.ChainIdList.Data.Contains(input.TargetChainId), "Not support target chain.");
        // TODO: validate receiver address by chain
        Assert(input.Receiver != null && input.Receiver != ByteString.Empty, "Invalid receiver.");
        Assert(input.Message != null && input.Message != ByteString.Empty, "Can't cross chain transfer empty message.");
        if (input.TokenTransferMetadata != null) ValidateTokenAmountInput(input);

        var messageInfo = new MessageInfo
        {
            SourceChainId = Context.ChainId,
            TargetChainId = input.TargetChainId,
            Sender = Context.Sender.ToByteString(),
            Receiver = input.Receiver,
            Message = input.Message,
            TokenTransferMetadata = input.TokenTransferMetadata,
            Created = Context.CurrentBlockTime
        };
        var messageId = HashHelper.ComputeFrom(messageInfo);
        Assert(State.MessageInfoMap[messageId] == null, "This message was send.");
        messageInfo.MessageId = messageId;
        State.MessageInfoMap[messageId] = messageInfo;

        var latestEpoch = State.LatestEpoch.Value;
        State.LatestEpoch.Value = State.LatestEpoch.Value.Add(1);

        Context.Fire(new SendRequested
        {
            MessageId = messageId,
            TargetChainId = input.TargetChainId,
            Sender = Context.Sender,
            Receiver = input.Receiver,
            Message = input.Message,
            TokenTransferMetadata = input.TokenTransferMetadata,
            Epoch = latestEpoch
        });

        return new Empty();
    }

    public override Empty Commit(CommitInput input)
    {
        CheckInitialized();

        Assert(input != null, "Invalid commit input.");
        Assert(input.Report != null, "Invalid report input.");
        VerifyReportContext(input.Report.ReportContext);
        VerifyTransmitter();
        VerifyThresholdSignature(input);

        var reportContext = input.Report.ReportContext;
        var messageId = reportContext.MessageId;
        Assert(State.ReceivedMessageInfoMap[messageId] == null,
            $"The same message {messageId} cannot be forwarded twice.");

        Context.SendInline(Address.Parser.ParseFrom(reportContext.Receiver),
            nameof(RampInterfaceContainer.RampInterfaceReferenceState.ForwardMessage), new ForwardMessageInput
            {
                SourceChainId = reportContext.SourceChainId,
                TargetChainId = reportContext.TargetChainId,
                Sender = reportContext.Sender,
                Receiver = reportContext.Receiver,
                Message = input.Report.Message,
                TokenTransferMetadata = input.Report.TokenTransferMetadata
            });

        State.ReceivedMessageInfoMap[messageId] = HashHelper.ComputeFrom(input);

        Context.Fire(new CommitReportAccepted { Report = input.Report });

        return new Empty();
    }

    public override Empty Cancel(Hash input)
    {
        CheckInitialized();
        CheckAdminPermission();
        Assert(IsHashValid(input) && State.MessageInfoMap[input] != null, "This message id is invalid.");

        Context.Fire(new RequestCancelled { MessageId = input });
        return new Empty();
    }

    public override Empty BatchManuallyExecute(ManuallyExecuteRequestsInput input)
    {
        CheckInitialized();
        CheckAdminPermission();
        Assert(input.RequestList is { Requests.Count: > 0 }, "Invalid manually execute request list.");

        foreach (var messageId in input.RequestList.Requests)
        {
            Assert(IsHashValid(messageId) && State.MessageInfoMap[messageId] != null,
                $"This message id {messageId} is invalid.");
            Context.Fire(new RequestManuallyExecuted { MessageId = messageId });
        }

        return new Empty();
    }

    private void VerifyReportContext(ReportContext context)
    {
        Assert(context != null, "Invalid report context.");
        Assert(IsHashValid(context.MessageId), "Invalid message id.");
        Assert(Context.ChainId == context.TargetChainId, "Unmatched chain id.");
        Assert(context.Receiver != null && Address.Parser.ParseFrom(context.Receiver) != null &&
               IsAddressValid(Address.Parser.ParseFrom(context.Receiver)), "Invalid receiver address.");
    }

    private void VerifyTransmitter()
    {
        var transmitters = State.OracleContract.GetTransmitters.Call(new Empty());
        Assert(transmitters.Data.Contains(Context.Sender), "Invalid transmitter");
    }

    private void VerifyThresholdSignature(CommitInput input)
    {
        var getConfigOutput = State.OracleContract.GetConfig.Call(new Empty());
        Assert(input.Signatures.Count >= (getConfigOutput.Config.N + getConfigOutput.Config.F) / 2 + 1,
            "Not enough signatures.");

        var hash = HashHelper.ComputeFrom(input.Report.ToByteArray());
        HashSet<Address> signed = new();
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

    private void ValidateTokenAmountInput(SendInput input)
    {
        var tokenAmount = input.TokenTransferMetadata;
        Assert(tokenAmount.TargetChainId > 0 && tokenAmount.TargetChainId == input.TargetChainId,
            "Invalid target chainId.");
        Assert(!string.IsNullOrEmpty(tokenAmount.Symbol), "Invalid OriginToken.");
    }
}
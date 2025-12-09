using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.Ramp;

public partial class RampContract
{
    public override Config GetConfig(Empty input) => State.Config.Value;
    public override Address GetAdmin(Empty input) => State.Admin.Value;
    public override RampSenderInfo GetRampSender(Address senderAddress) => State.RampSenders[senderAddress];
    public override Address GetOracleContractAddress(Empty input) => State.OracleContract.Value;
    public override Int64Value GetLatestEpoch(Empty input) => new() { Value = State.LatestEpoch.Value };
    public override Hash GetMessageMetaData(Hash messageId) => State.ReceivedMessageInfoMap[messageId];
}
using AElf;
using AElf.Sdk.CSharp;
using AElf.Types;
using AetherLink.Contracts.Upkeep;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.Automation.Upkeep;

public class UpkeepContract : UpkeepContractContainer.UpkeepContractBase
{
    public override Empty PerformUpkeep(PerformUpkeepInput input)
    {
        Assert(input != null, "input is null");
        Assert(input.PerformData != null, "PerformData is null");

        var checkData = LogTriggerCheckData.Parser.ParseFrom(input.PerformData);

        Context.Fire(new Triggered
        {
            ChainId = checkData.ChainId,
            BlockHeight = checkData.BlockHeight,
            EventName = checkData.EventName,
            Index = checkData.Index
        });

        return new Empty();
    }

    public override Empty CreateMockEvent(Hash data)
    {
        Context.Fire(new LogEventCreated
        {
            MockData = data
        });
        return new Empty();
    }
}
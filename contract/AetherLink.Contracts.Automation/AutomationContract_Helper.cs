using AElf;
using AElf.Types;

namespace AetherLink.Contracts.Automation;

public partial class AutomationContract
{
    private void CheckAdminPermission() => Assert(Context.Sender == State.Admin.Value, "No permission.");
    private void CheckInitialized() => Assert(State.Initialized.Value, "Not initialized.");
    private void CheckUnpause() => Assert(!State.Paused.Value, "Contract paused.");
    private bool IsAddressValid(Address input) => input != null && !input.Value.IsNullOrEmpty();
    private bool IsHashValid(Hash input) => input != null && !input.Value.IsNullOrEmpty();

    private void CheckOracleContractPermission() =>
        Assert(Context.Sender == State.OracleContract.Value, "Only OracleContract has permission.");

    private void CheckUpkeepAdminPermission(Hash upkeepId) =>
        Assert(Context.Sender == State.RegisteredUpkeepMap[upkeepId].AdminAddress, "Not upkeep admin.");
}
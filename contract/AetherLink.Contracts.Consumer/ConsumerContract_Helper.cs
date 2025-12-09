using AElf;
using AElf.Types;

namespace AetherLink.Contracts.Consumer;

public partial class ConsumerContract
{
    private void CheckAdminPermission()
    {
        Assert(Context.Sender == State.Admin.Value, "No permission.");
    }

    private void CheckOraclePermission()
    {
        Assert(Context.Sender == State.OracleContract.Value, "No permission.");
    }

    private bool IsHashValid(Hash input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }

    private bool IsAddressValid(Address input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }

    private void CheckControllerPermission()
    {
        Assert(State.Controller.Value != null && State.Controller.Value.Data.Count > 0, "Controller not set.");
        Assert(State.Controller.Value.Data.Contains(Context.Sender), "No permission.");
    }
}
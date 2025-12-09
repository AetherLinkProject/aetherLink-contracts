using AElf;
using AElf.Types;

namespace AetherLink.Contracts.VRF.Coordinator;

public partial class VrfCoordinatorContract
{
    private void CheckAdminPermission()
    {
        Assert(Context.Sender == State.Admin.Value, "No permission.");
    }

    private void CheckOracleContractPermission()
    {
        Assert(Context.Sender == State.OracleContract.Value, "No permission.");
    }

    private void CheckInitialized()
    {
        Assert(State.Initialized.Value, "Not initialized.");
    }

    private void CheckUnpause()
    {
        Assert(!State.Paused.Value, "Contract paused.");
    }

    private bool IsAddressValid(Address input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }

    private bool IsHashValid(Hash input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }
}
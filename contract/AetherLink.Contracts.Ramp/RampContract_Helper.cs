using AElf;
using AElf.Types;

namespace AetherLink.Contracts.Ramp;

public partial class RampContract
{
    private void CheckInitialized() => Assert(State.Initialized.Value, "Not initialized.");
    private void CheckAdminPermission() => Assert(Context.Sender == State.Admin.Value, "No permission.");
    private bool IsAddressValid(Address input) => input != null && !input.Value.IsNullOrEmpty();
    private bool IsHashValid(Hash input) => input != null && !input.Value.IsNullOrEmpty();
}
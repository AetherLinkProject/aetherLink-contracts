using AElf;
using AElf.Types;

namespace AetherLink.Contracts.Oracle;

public partial class OracleContract
{
    private void CheckAdminPermission()
    {
        Assert(Context.Sender == State.Admin.Value, "No permission.");
    }

    private void CheckInitialized()
    {
        Assert(State.Initialized.Value, "Not initialized.");
    }

    private bool IsAddressValid(Address input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }

    private bool IsHashValid(Hash input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }

    private Hash ComputeHashOfKey(string publicKey)
    {
        return HashHelper.ComputeFrom(publicKey);
    }

    private void CheckTransmitterPermission()
    {
        var transmitter = State.OraclesMap[Context.Sender];
        Assert(transmitter != null && transmitter.Role == Role.Transmitter, "Not transmitter.");
    }

    private void CheckCoordinatorContractPermission(int requestTypeIndex)
    {
        var coordinator = State.Coordinators[requestTypeIndex];
        Assert(coordinator != null && coordinator.Status, "Unauthorized coordinator contract.");
        Assert(Context.Sender == coordinator.CoordinatorContractAddress, "No permission.");
    }

    private void CheckUnpause()
    {
        Assert(!State.Paused.Value, "Contract paused.");
    }
}
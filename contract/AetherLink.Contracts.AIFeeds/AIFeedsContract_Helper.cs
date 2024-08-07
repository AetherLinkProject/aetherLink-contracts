using AElf;
using AElf.Types;

namespace AetherLink.Contracts.AIFeeds;

public partial class AIFeedsContract
{
    private bool IsAddressValid(Address input) => input != null && !input.Value.IsNullOrEmpty();
    private bool IsHashValid(Hash input) => input != null && !input.Value.IsNullOrEmpty();
    private bool IsAdminValid() => Context.Sender == State.Admin.Value;
}
using AElf.Kernel.Infrastructure;
using AElf.Types;

namespace AElf.Boilerplate.TestBase.SmartContractNameProviders;

public class DataFeedsCoordinatorContractAddressNameProvider
{
    public static readonly Hash Name = HashHelper.ComputeFrom("AetherLink.Contracts.DataFeeds.Coordinator");

    public static readonly string StringName = Name.ToStorageKey();
    public Hash ContractName => Name;
    public string ContractStringName => StringName;
}
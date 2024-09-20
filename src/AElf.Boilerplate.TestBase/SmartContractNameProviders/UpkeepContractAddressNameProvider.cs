using AElf.Kernel.Infrastructure;
using AElf.Types;

namespace AElf.Boilerplate.TestBase.SmartContractNameProviders;

public class UpkeepContractAddressNameProvider
{
    public static readonly Hash Name = HashHelper.ComputeFrom("AetherLink.Contracts.Automation.Upkeep");

    public static readonly string StringName = Name.ToStorageKey();
    public Hash ContractName => Name;
    public string ContractStringName => StringName;
}
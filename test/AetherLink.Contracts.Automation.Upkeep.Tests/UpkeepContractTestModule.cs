using AElf.Boilerplate.TestBase;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AetherLink.Contracts.Automation.Upkeep.ContractInitializationProvider;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AetherLink.Contracts.Automation.Upkeep;

[DependsOn(typeof(MainChainDAppContractTestModule))]
public class UpkeepContractTestModule : MainChainDAppContractTestModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services
            .AddSingleton<IContractInitializationProvider, UpkeepContractInitializationProvider>();
        Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
    }
}
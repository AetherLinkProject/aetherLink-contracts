using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AetherLink.Contracts.Oracle;

public partial class OracleContractState : ContractState
{
    // admin
    public SingletonState<bool> Initialized { get; set; }
    public SingletonState<Address> Admin { get; set; }
    public SingletonState<Address> PendingAdmin { get; set; }
    public SingletonState<bool> Paused { get; set; }

    // config
    public SingletonState<long> MaxOracleCount { get; set; }
    public SingletonState<long> LatestConfigBlockNumber { get; set; } // easy to extract config from logs
    public SingletonState<long> LatestRound { get; set; }
    public SingletonState<long> ConfigCount { get; set; } // prevent replay attacks
    public SingletonState<Config> Config { get; set; }
    public MappedState<Address, Oracle> OraclesMap { get; set; }
    public SingletonState<AddressList> Signers { get; set; }
    public SingletonState<AddressList> Transmitters { get; set; }
    public MappedState<int, Coordinator> Coordinators { get; set; }
    public SingletonState<SubscriptionConfig> SubscriptionConfig { get; set; }
    public SingletonState<int> CurrentRequestTypeIndex { get; set; }

    // public key
    public MappedState<Hash, Address> ProvingKeyOraclesMap { get; set; }
    public SingletonState<HashList> ProvingKeyHashes { get; set; }

    // Subscription
    public SingletonState<long> CurrentSubscriptionId { get; set; }
    public MappedState<long, Subscription> Subscriptions { get; set; }
    public MappedState<Address, long, Consumer> Consumers { get; set; }
}
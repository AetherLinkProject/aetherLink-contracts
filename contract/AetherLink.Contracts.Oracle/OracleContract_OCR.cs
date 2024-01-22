using AElf;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.Oracle;

public partial class OracleContract
{
    public override Empty SetMaxOracleCount(Int64Value input)
    {
        CheckAdminPermission();
        Assert(input != null, "Invalid input.");
        Assert(input.Value > 0, "Must be positive.");

        State.MaxOracleCount.Value = input.Value;

        return new Empty();
    }

    public override Empty SetConfig(SetConfigInput input)
    {
        CheckAdminPermission();
        ValidateSetConfigInput(input);

        BeforeSetConfig();

        for (var i = 0; i < input.Signers.Count; i++)
        {
            SetRole(input.Signers[i], i, Role.Signer);
            SetRole(input.Transmitters[i], i, Role.Transmitter);
        }

        State.Signers.Value.Data.AddRange(input.Signers);
        State.Transmitters.Value.Data.AddRange(input.Transmitters);

        var previousConfigBlockNumber = State.LatestConfigBlockNumber.Value;
        State.LatestConfigBlockNumber.Value = Context.CurrentHeight;
        State.ConfigCount.Value = State.ConfigCount.Value.Add(1);
        State.LatestRound.Value = 0;

        var latestConfigDigest = HashHelper.ComputeFrom(new ConfigData
        {
            ChainId = Context.ChainId,
            ContractAddress = Context.Self,
            ConfigCount = State.ConfigCount.Value,
            Signers = { State.Signers.Value.Data },
            Transmitters = { State.Transmitters.Value.Data },
            F = input.F,
            OffChainConfig = input.OffChainConfig,
            OffChainConfigVersion = input.OffChainConfigVersion
        });

        State.Config.Value = new Config
        {
            F = input.F,
            N = input.Signers.Count,
            LatestConfigDigest = latestConfigDigest
        };

        Context.Fire(new ConfigSet
        {
            PreviousConfigBlockNumber = previousConfigBlockNumber,
            ConfigDigest = latestConfigDigest,
            ConfigCount = State.ConfigCount.Value,
            Signers = State.Signers.Value,
            Transmitters = State.Transmitters.Value,
            F = State.Config.Value.F,
            OffChainConfig = input.OffChainConfig,
            OffChainConfigVersion = input.OffChainConfigVersion
        });

        return new Empty();
    }

    private void SetRole(Address oracle, int index, Role role)
    {
        Assert(IsAddressValid(oracle), "Invalid " + role + " address.");
        Assert(State.OraclesMap[oracle] == null || State.OraclesMap[oracle].Role == Role.Unset,
            "Repeated " + role + " address.");
        State.OraclesMap[oracle] = new Oracle
        {
            Index = index,
            Role = role
        };
    }

    public override Empty RegisterProvingKey(RegisterProvingKeyInput input)
    {
        CheckAdminPermission();
        Assert(input != null, "Invalid input.");
        Assert(IsAddressValid(input.Oracle), "Invalid input oracle.");
        Assert(!string.IsNullOrWhiteSpace(input.PublicProvingKey), "Invalid input public proving key.");

        var hash = ComputeHashOfKey(input.PublicProvingKey);

        if (State.ProvingKeyOraclesMap[hash] != null)
        {
            return new Empty();
        }

        State.ProvingKeyOraclesMap[hash] = input.Oracle;
        State.ProvingKeyHashes.Value ??= new HashList();

        State.ProvingKeyHashes.Value.Data.Add(hash);

        Context.Fire(new ProvingKeyRegistered
        {
            Oracle = input.Oracle,
            KeyHash = hash
        });

        return new Empty();
    }

    public override Empty DeregisterProvingKey(DeregisterProvingKeyInput input)
    {
        CheckAdminPermission();
        Assert(input != null, "Invalid input.");
        Assert(!string.IsNullOrWhiteSpace(input.PublicProvingKey), "Invalid input public proving key.");

        var hash = ComputeHashOfKey(input.PublicProvingKey);

        if (State.ProvingKeyOraclesMap[hash] == null)
        {
            return new Empty();
        }

        var oracleToRemove = State.ProvingKeyOraclesMap[hash].Clone();
        State.ProvingKeyOraclesMap.Remove(hash);
        State.ProvingKeyHashes.Value.Data.Remove(hash);

        Context.Fire(new ProvingKeyDeregistered
        {
            Oracle = oracleToRemove,
            KeyHash = hash
        });

        return new Empty();
    }

    private void ValidateSetConfigInput(SetConfigInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(input.Signers != null, "Invalid input signers.");
        Assert(input.Transmitters != null, "Invalid input transmitters.");

        Assert(input.Signers.Count <= State.MaxOracleCount.Value, "Too many signers.");
        Assert(input.F > 0, "f must be positive.");
        Assert(input.Signers.Count == input.Transmitters.Count, "Oracle addresses out of registration.");
        Assert(input.Signers.Count >= 3 * input.F, "Faulty-oracle f too high.");
    }

    private void BeforeSetConfig()
    {
        // Clear previous settings
        if (State.ConfigCount.Value > 0)
        {
            var signers = State.Signers.Value.Data;
            var transmitters = State.Transmitters.Value.Data;

            for (var i = 0; i < signers.Count; i++)
            {
                State.OraclesMap.Remove(signers[i]);
                State.OraclesMap.Remove(transmitters[i]);
            }
        }

        State.Signers.Value = new AddressList();
        State.Transmitters.Value = new AddressList();
    }

    private void ValidateStartRequestInput(StartRequestInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(IsHashValid(input.RequestId), "Invalid input request id.");
        Assert(IsAddressValid(input.RequestingContract), "Invalid input requesting contract.");
        Assert(input.SubscriptionId > 0, "Invalid input subscription id.");
        Assert(IsAddressValid(input.SubscriptionOwner), "Invalid input subscription owner.");
        Assert(!input.Commitment.IsNullOrEmpty(), "Invalid input commitment.");
        Assert(input.RequestTypeIndex > 0, "Invalid input request type index.");
    }
}
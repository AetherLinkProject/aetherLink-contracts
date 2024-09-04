using AElf;
using AElf.Sdk.CSharp;
using AElf.Types;
using Ai;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.AIFeeds.Demo;

public partial class AIFeedsDemoContract
{
    public override Empty Adopt(Empty input)
    {
        var attributes = new Attributes();
        attributes.Attribute.Add(new Attribute
        {
            TraitType = "Personality",
            Value = "cute"
        });
        attributes.Attribute.Add(new Attribute
        {
            TraitType = "Coat Color",
            Value = "black"
        });
        attributes.Attribute.Add(new Attribute
        {
            TraitType = "Breeds",
            Value = "bully"
        });

        var adopted = new Adopted
        {
            Adopter = Context.Sender,
            Attributes = attributes,
            BlockHeight = Context.CurrentHeight
        };

        var adoptId = HashHelper.ComputeFrom(adopted);
        adopted.AdoptId = adoptId;
        State.InscriptionInfoMap[adoptId] = new() { Traits = attributes };
        var description = new Description
        {
            Type = DescriptionType.String,
            Detail = ByteString.CopyFromUtf8(
                $@"a {attributes.Attribute[0].Value} {attributes.Attribute[1].Value} {attributes.Attribute[2].Value} dog.
The dog wears a red collar with a silver tag engraved with its name. 
It is sitting in a grassy park, surrounded by blooming flowers, with the sun casting a soft glow on its fur. 
The dog looks playful and energetic, ready to fetch a ball or run around with its owner.")
        };

        State.AIFeedsContract.StartAIRequest.Send(new()
        {
            TraceId = adoptId,
            Admin = Context.Sender,
            Model = ModelType.Dall,
            FulfillAddress = Context.Self,
            Description = description
        });

        Context.Fire(adopted);

        return new Empty();
    }

    public override InscriptionInfo GetInscription(Hash input) => State.InscriptionInfoMap[input];

    public override Empty HandleAIFeedsFulfillment(HandleAIFeedsFulfillmentInput input)
    {
        Assert(State.InscriptionInfoMap[input.TraceId] != null, $"Not exist traceId {input.TraceId}.");
        var imageBase64 = ChatGptResponse.Parser.ParseFrom(input.Response).Content.ToStringUtf8();
        var inscriptionInfo = State.InscriptionInfoMap[input.TraceId];
        inscriptionInfo.Image = imageBase64;
        inscriptionInfo.Valuation = Context.ConvertHashToInt64(GetRandomHash(imageBase64), 1, 10000000);

        return new Empty();
    }

    private Hash GetRandomHash(string seed)
    {
        if (State.ConsensusContract.Value == null)
        {
            State.ConsensusContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
        }

        var hashResult = State.ConsensusContract.GetRandomHash.Call(new Int64Value { Value = Context.CurrentHeight });
        return HashHelper.ConcatAndCompute(hashResult, HashHelper.ComputeFrom(seed));
    }
}
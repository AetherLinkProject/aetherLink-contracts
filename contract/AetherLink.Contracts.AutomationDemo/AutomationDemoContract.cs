using AElf;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AetherLink.Contracts.Automation;
using AetherLink.Contracts.Upkeep;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.AutomationDemo;

public class AutomationDemoContract : AutomationDemoContractContainer.AutomationDemoContractBase
{
    private const string CronJobSpec =
        "{\"Cron\": \"0 0 0 1/1 * ? \",\"TriggerDataSpec\": {\"TriggerType\": \"Cron\"}}";

    public override Empty PerformUpkeep(PerformUpkeepInput input)
    {
        var record = OrderRecord.Parser.ParseFrom(input.PerformData);
        var consumer = record.Consumer;
        var purchaseQuantity = State.OrderRecordMap[HashHelper.ComputeFrom(record)];
        var investmentInfo = State.InvestmentInfoMap[record.InvestmentName];
        var transferAmount = purchaseQuantity.Amount.Mul(investmentInfo.DailyInterestRate.Div(100));

        State.TokenContract.Transfer.Send(new TransferInput
        {
            To = consumer,
            Symbol = investmentInfo.RewardCurrencyName,
            Amount = transferAmount
        });

        Context.Fire(new RewardsTransferred
        {
            Beneficiary = consumer,
            InvestmentName = record.InvestmentName,
            Amount = transferAmount
        });

        return new Empty();
    }

    public override Empty BuyInvestment(BuyInvestmentInput input)
    {
        var record = new OrderRecord
        {
            Consumer = Context.Sender,
            InvestmentName = input.InvestmentName,
            Amount = input.Amount,
            Created = Context.CurrentBlockTime
        };

        State.AutomationContract.RegisterUpkeep.Send(new RegisterUpkeepInput
        {
            Name = $"{input.InvestmentName}-{Context.Sender}-{Context.CurrentBlockTime}",
            UpkeepContract = Context.Self,
            AdminAddress = State.Admin.Value,
            TriggerType = TriggerType.Cron,
            TriggerData = ByteString.CopyFromUtf8(CronJobSpec),
            PerformData = record.ToByteString()
        });
        
        State.OrderRecordMap[HashHelper.ComputeFrom(record)] = record;

        Context.Fire(new InvestmentBought
        {
            Consumer = Context.Sender,
            InvestmentName = input.InvestmentName,
            InvestmentPrice = State.InvestmentInfoMap[input.InvestmentName].InvestmentPrice,
            RewardCurrencyName = State.InvestmentInfoMap[input.InvestmentName].RewardCurrencyName,
            Amount = input.Amount
        });

        return new Empty();
    }
}
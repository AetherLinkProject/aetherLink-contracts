using System.Threading.Tasks;
using AetherLink.Contracts.Upkeep;
using Google.Protobuf;
using Oracle;
using Xunit;

namespace AetherLink.Contracts.Automation.Upkeep;

public partial class UpkeepContractTests : UpkeepContractTestBase
{
    [Fact]
    public async Task PerformTests()
    {
        var bs = ByteString.FromBase64(
            "CtUBCgR0RFZXEkBhMmMzNWQxOTU2MDYzNjg2OWJjOTc0N2JkZDE0M2MxM2EyODdhNTliZjcxMzQ2ZjkwODI4MWQxOGJiMTc5ODNlGPGA0kIiQGQyMWU3NDQ5YzgzZDRhYWE1ZDI1NjQzMzVkMzI4MDAxY2FiMmU5Yzg0MjJiNTM4ZTg1OTdiYjk5Zjg3NjQ2NmQqMURidTFpYjhZeXFTdzJXSkg1bUNQbzJTQ2k2cUxBWkVLTGY0Z1preHB0YUxZS1NvZ1gyD0xvZ0V2ZW50Q3JlYXRlZDgBGtUCCiIKIL1RIQP7sZT03VqrtLdydolcs0zLSEjwdc+e+s731J1sEiIKIODJ6PuHnvt8MCs+QHstbG+ZW4/6nEZ2rf35VCRc7hXVGiIKIFIbncC/h7MOYYA+z7F912nHu3YUEBnz81V9heOUSvcwIAcqCwjw8bS3BhDIgIlvMtUBCghURVNULTcwMxIiCiAcnRbUibTNcgXYxD5Gt30XbyTNQ1GsQ/eUq7S30iEfqxoiCiBSG53Av4ezDmGAPs+xfddpx7t2FBAZ8/NVfYXjlEr3MCABKnl7IkNoYWluSWQiOiAidERWVyIsIkNvbnRyYWN0QWRkcmVzcyI6ICJEYnUxaWI4WXlxU3cyV0pINW1DUG8yU0NpNnFMQVpFS0xmNGdaa3hwdGFMWUtTb2dYIiwiRXZlbnROYW1lIjogIkxvZ0V2ZW50Q3JlYXRlZCJ9MgR0ZXN0OAM=");
        var report = Report.Parser.ParseFrom(bs);
        var result = await UpkeepContractStub.PerformUpkeep.SendAsync(new PerformUpkeepInput
        {
            PerformData = report.Result
        });
    }
}
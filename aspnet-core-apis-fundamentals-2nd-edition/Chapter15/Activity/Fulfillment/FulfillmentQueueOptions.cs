using System.ComponentModel.DataAnnotations;
using System.Threading.Channels;

namespace WarehouseApi.Fulfillment;

public class FulfillmentQueueOptions
{
    public const string SectionName = "Fulfillment:Queue";

    [Range(1, 100_000)]
    public int Capacity { get; set; } = 1000;

    // Wait makes producers wait for space. The Drop modes discard a job instead; its shipment
    // then stays Pending in the database until the processor's next sweep or start
    public BoundedChannelFullMode FullMode { get; set; } = BoundedChannelFullMode.Wait;
}

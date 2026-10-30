namespace FakeErp;

public class FakeErpOptions
{
    public const string SectionName = "FakeErp";

    // The first N shipment requests fail with 503 before anything is created
    public int FailFirstRequests { get; set; }

    // The first N shipments are created, but the client receives 503 instead of the answer
    public int LoseFirstResponses { get; set; }

    // A slow ERP: every shipment request waits this long before it is processed
    public int DelayMilliseconds { get; set; }
}

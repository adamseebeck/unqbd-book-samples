namespace WarehouseApi.Services;

// What a stock replacement or deletion did; the endpoint turns it into a status code
public enum StockChangeOutcome
{
    Applied,
    NotFound,

    // The change would alter reserved units, which only the order workflows change
    ReservationConflict,

    // The stock item has a history, the audit trail of its changes, so it is never deleted
    HasHistory
}

namespace WarehouseApi.Contracts;

// RetrySuccessRate is null when no retry ran in the last hour
public record DeadLetterStatusResponse(
    int Depth,
    DateTime? OldestFailureAt,
    int RetriesLastHour,
    int SucceededLastHour,
    double? RetrySuccessRate);

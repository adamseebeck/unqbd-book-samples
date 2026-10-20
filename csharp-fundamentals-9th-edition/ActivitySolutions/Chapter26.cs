using System.Globalization;

PaymentResult paid = Pay(19.95m);
PaymentResult declined = Pay(0m);
PaymentResult empty = default;
Console.WriteLine(DescribeResult(paid));
Console.WriteLine(DescribeResult(declined));
Console.WriteLine(DescribeResult(empty));

Console.WriteLine(DescribeState(new PendingPayment()));
Console.WriteLine(DescribeState(new FinishedPayment("R-100")));
Console.WriteLine(DescribeState(null));

static PaymentResult Pay(decimal amount)
{
    if (amount <= 0)
        return new Declined("Amount must be positive");
    return new Paid("R-100", amount);
}

static string DescribeResult(PaymentResult result) => result switch
{
    Paid(var receipt, var amount) =>
        $"Paid {receipt}: {amount.ToString("F2", CultureInfo.InvariantCulture)}",
    Declined(var reason) => $"Declined: {reason}",
    null => "No result"
};

static string DescribeState(PaymentState? state) => state switch
{
    PendingPayment => "Pending",
    FinishedPayment(var receipt) => $"Finished: {receipt}",
    null => "No state"
};

public sealed record Paid(string Receipt, decimal Amount);
public sealed record Declined(string Reason);
public union PaymentResult(Paid, Declined);

public closed record PaymentState;
public sealed record PendingPayment : PaymentState;
public sealed record FinishedPayment(string Receipt) : PaymentState;

public readonly record struct Money(decimal Value);

public abstract class Payment
{
    public int Id { get; set; }
    public Money Amount { get; set; }
}

public sealed class CardPayment : Payment
{
    public string LastFour { get; set; } = "";
}

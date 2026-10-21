namespace MauiInventory.App;
public partial class TemplatedNotice : TemplatedView
{
    public static readonly BindableProperty HeadingProperty = BindableProperty.Create(nameof(Heading), typeof(string), typeof(TemplatedNotice), "");
    public static readonly BindableProperty MessageProperty = BindableProperty.Create(nameof(Message), typeof(string), typeof(TemplatedNotice), "");
    public string Heading { get => (string)GetValue(HeadingProperty); set => SetValue(HeadingProperty, value); }
    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }

    public TemplatedNotice() => InitializeComponent();
}

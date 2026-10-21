using Microsoft.Maui.Handlers;

namespace MauiInventory.App;
public sealed class BorderlessEntry : Entry
{
}

public sealed class BorderlessEntryHandler : EntryHandler
{
#if ANDROID
    protected override void ConnectHandler(Microsoft.Maui.Platform.MauiAppCompatEditText view)
    { base.ConnectHandler(view);view.BackgroundTintList=Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent); }
#elif IOS || MACCATALYST
    protected override void ConnectHandler(Microsoft.Maui.Platform.MauiTextField view)
    { base.ConnectHandler(view);view.BorderStyle=UIKit.UITextBorderStyle.None; }
#elif WINDOWS
    protected override void ConnectHandler(Microsoft.UI.Xaml.Controls.TextBox view)
    { base.ConnectHandler(view);view.BorderThickness=new Microsoft.UI.Xaml.Thickness(0); }
#endif
}

public sealed class GradientButton : Button
{
}

public sealed class GradientButtonHandler : ButtonHandler
{
#if ANDROID
    private Android.Graphics.Drawables.GradientDrawable? background;
    protected override void ConnectHandler(Google.Android.Material.Button.MaterialButton view)
    {
        base.ConnectHandler(view);
        background=new Android.Graphics.Drawables.GradientDrawable(
            Android.Graphics.Drawables.GradientDrawable.Orientation.LeftRight,
            [Android.Graphics.Color.Rgb(20,93,128).ToArgb(),Android.Graphics.Color.Rgb(29,122,83).ToArgb()]);
        view.Background=background;
    }
    protected override void DisconnectHandler(Google.Android.Material.Button.MaterialButton view)
    { view.Background=null;background?.Dispose();background=null;base.DisconnectHandler(view); }
#elif IOS || MACCATALYST
    private CoreAnimation.CAGradientLayer? gradient;
    protected override void ConnectHandler(UIKit.UIButton view)
    {
        base.ConnectHandler(view);gradient=new CoreAnimation.CAGradientLayer {
            Colors=[UIKit.UIColor.FromRGB(20,93,128).CGColor,UIKit.UIColor.FromRGB(29,122,83).CGColor] };
        view.Layer.InsertSublayer(gradient,0);if(VirtualView is VisualElement element)element.SizeChanged+=OnSizeChanged;UpdateFrame();
    }
    private void OnSizeChanged(object? sender,EventArgs e)=>UpdateFrame();
    private void UpdateFrame() { if(gradient is not null)gradient.Frame=PlatformView.Bounds; }
    protected override void DisconnectHandler(UIKit.UIButton view)
    { if(VirtualView is VisualElement element)element.SizeChanged-=OnSizeChanged;gradient?.RemoveFromSuperLayer();gradient?.Dispose();gradient=null;base.DisconnectHandler(view); }
#endif
}

public sealed class QuantityPicker : ContentView
{
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(int), typeof(QuantityPicker), 0, BindingMode.TwoWay, coerceValue: (_, value) => Math.Max(0, (int)value), propertyChanged: (b, _, _) => ((QuantityPicker)b).UpdateText());
    private readonly Label valueLabel = new()
    {
        VerticalOptions = LayoutOptions.Center
    };
    public int Value { get => (int)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public QuantityPicker()
    {
        var minus = new Button
        {
            Text = "−"
        };
        var plus = new Button
        {
            Text = "+"
        };
        SemanticProperties.SetDescription(minus, "Decrease quantity");
        SemanticProperties.SetDescription(plus, "Increase quantity");
        minus.Clicked += (_, _) => Value = Math.Max(0, Value - 1);
        plus.Clicked += (_, _) => Value = Value == int.MaxValue ? Value : Value + 1;
        Content = new HorizontalStackLayout
        {
            Spacing = 12,
            Children =
            {
                minus,
                valueLabel,
                plus
            }
        };
        UpdateText();
    }

    private void UpdateText() => valueLabel.Text = Value.ToString(System.Globalization.CultureInfo.CurrentCulture);
}

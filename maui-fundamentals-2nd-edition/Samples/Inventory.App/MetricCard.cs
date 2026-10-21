using Microsoft.Maui.Handlers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MauiInventory.App;
public enum TrendDirection
{
    Up,
    Down,
    Neutral
}

public sealed class MetricBorder : Border
{
}

public sealed class MetricCard : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(MetricCard), "", propertyChanged: Changed);
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(string), typeof(MetricCard), "", propertyChanged: Changed);
    public static readonly BindableProperty TrendDirectionProperty = BindableProperty.Create(nameof(TrendDirection), typeof(TrendDirection), typeof(MetricCard), TrendDirection.Neutral, propertyChanged: Changed);
    private readonly Label titleLabel = new();
    private readonly Label valueLabel = new()
    {
        FontSize = 24
    };
    private readonly Label trendLabel = new();
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public TrendDirection TrendDirection { get => (TrendDirection)GetValue(TrendDirectionProperty); set => SetValue(TrendDirectionProperty, value); }

    public MetricCard()
    {
        var border = new MetricBorder
        {
            Padding = 16,
            Margin = 8,
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    titleLabel,
                    valueLabel,
                    trendLabel
                }
            }
        };
        border.SetAppThemeColor(BackgroundColorProperty, Colors.White, Color.FromArgb("#202020"));
        Content = border;
        Update();
    }

    private static void Changed(BindableObject bindable, object oldValue, object newValue) => ((MetricCard)bindable).Update();
    private void Update()
    {
        titleLabel.Text = Title;
        valueLabel.Text = Value;
        trendLabel.Text = $"Trend: {TrendDirection}";
    }
}

public static class ShadowExtensions
{
    private static int registered;
    public static void ApplyMetricCardShadows()
    {
        if (Interlocked.Exchange(ref registered, 1) != 0)
            return;
        BorderHandler.Mapper.AppendToMapping("MetricCardShadow", (handler, view) =>
        {
            if (view is not MetricBorder)
                return;
#if ANDROID
            handler.PlatformView.Elevation=8*(handler.PlatformView.Context?.Resources?.DisplayMetrics?.Density??1);
#elif IOS || MACCATALYST
            handler.PlatformView.Layer.ShadowColor=UIKit.UIColor.Black.CGColor;
            handler.PlatformView.Layer.ShadowOpacity=0.2f;handler.PlatformView.Layer.ShadowRadius=6;
            handler.PlatformView.Layer.ShadowOffset=new CoreGraphics.CGSize(0,3);handler.PlatformView.Layer.MasksToBounds=false;
#endif
        });
    }
}

public sealed class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        Title = "Metric cards";
        var metrics = new DashboardMetrics();
        BindingContext = metrics;
        var stack = new VerticalStackLayout();
        var products = new MetricCard
        {
            Title = "Products",
            TrendDirection = TrendDirection.Up
        };
        products.SetBinding(MetricCard.ValueProperty, nameof(metrics.ProductCount));
        stack.Children.Add(products);
        var low = new MetricCard
        {
            Title = "Low stock",
            TrendDirection = TrendDirection.Down
        };
        low.SetBinding(MetricCard.ValueProperty, nameof(metrics.LowStockCount));
        stack.Children.Add(low);
        var pending = new MetricCard
        {
            Title = "Pending sync",
            TrendDirection = TrendDirection.Neutral
        };
        pending.SetBinding(MetricCard.ValueProperty, nameof(metrics.PendingCount));
        stack.Children.Add(pending);
        var update = new Button
        {
            Text = "Change pending count only"
        };
        update.Clicked += (_, _) => metrics.PendingCount = (int.Parse(metrics.PendingCount) + 1).ToString();
        stack.Children.Add(update);
        stack.Children.Add(new TemplatedNotice { Heading = "Template contract", Message = "This notice binds its declared Heading and Message properties." });
        Content = new ScrollView
        {
            Content = stack
        };
    }
}

public partial class DashboardMetrics : ObservableObject
{
    [ObservableProperty]
    public partial string ProductCount { get; set; }

    [ObservableProperty]
    public partial string LowStockCount { get; set; }

    [ObservableProperty]
    public partial string PendingCount { get; set; }

    public DashboardMetrics()
    {
        ProductCount = "3";
        LowStockCount = "1";
        PendingCount = "0";
    }
}

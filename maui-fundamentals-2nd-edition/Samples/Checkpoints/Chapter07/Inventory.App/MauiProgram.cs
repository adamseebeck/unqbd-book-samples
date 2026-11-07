using MauiInventory.Core;
using Microsoft.Extensions.DependencyInjection;

namespace MauiInventory.App;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder().UseMauiApp<App>();
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(BorderlessEntry), typeof(BorderlessEntryHandler));
            handlers.AddHandler(typeof(GradientButton), typeof(GradientButtonHandler));
        });
        builder.Services.AddSingleton<IProductRepository, MemoryProductRepository>();
        builder.Services.AddTransient<ProductListViewModel>();
        builder.Services.AddTransient<ProductsPage>();
        builder.Services.AddTransient<DetailsPage>();
        builder.Services.AddSingleton<WorkflowState>();
        builder.Services.AddSingleton<IWorkflowNavigationService, WorkflowNavigationService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<SecureStatePersistenceService>();
        builder.Services.AddTransient<CartPage>();
        builder.Services.AddTransient<CheckoutViewModel>();
        builder.Services.AddTransient<CheckoutPage>();
        builder.Services.AddTransient<ConfirmationPage>();
        builder.Services.AddSingleton<AppShell>();
        return builder.Build();
    }
}

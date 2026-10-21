using MauiInventory.Core;
using Microsoft.Extensions.DependencyInjection;

namespace MauiInventory.App;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder().UseMauiApp<App>();
        ShadowExtensions.ApplyMetricCardShadows();
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(BorderlessEntry), typeof(BorderlessEntryHandler));
            handlers.AddHandler(typeof(GradientButton), typeof(GradientButtonHandler));
        });
        builder.Services.AddSingleton<IProductRepository, MemoryProductRepository>();
        builder.Services.AddSingleton(_ => new SqliteProductRepository(Path.Combine(FileSystem.AppDataDirectory, "inventory-learning.db3")));
        builder.Services.AddTransient<ProductListViewModel>();
        builder.Services.AddTransient<ProductsPage>();
        builder.Services.AddTransient<DetailsPage>();
        builder.Services.AddTransient<ProductDetailViewModel>();
        builder.Services.AddTransient<ProductDetailActivityPage>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddSingleton(_ => new CategoryRepository(Path.Combine(FileSystem.AppDataDirectory, "categories-learning.db3")));
        builder.Services.AddSingleton<ICategoryRepository>(provider => provider.GetRequiredService<CategoryRepository>());
        builder.Services.AddSingleton<ITextFileStore, AppTextFileStore>();
        builder.Services.AddSingleton<IExportSettings, ExportSettings>();
        builder.Services.AddTransient<CategoryViewModel>();
        builder.Services.AddTransient<CategoriesPage>();
        builder.Services.AddSingleton(_ => new SqliteSyncQueue(Path.Combine(FileSystem.AppDataDirectory, "queue-learning.db3")));
        builder.Services.AddTransient<QueueActivityPage>();
        builder.Services.AddSingleton<IConnectivityStatus, MauiConnectivityStatus>();
        builder.Services.AddSingleton<WeakMessageService>();
        builder.Services.AddSingleton<WorkflowState>();
        builder.Services.AddSingleton<IWorkflowNavigationService, WorkflowNavigationService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<SecureStatePersistenceService>();
        builder.Services.AddTransient<CartPage>();
        builder.Services.AddTransient<CheckoutViewModel>();
        builder.Services.AddTransient<CheckoutPage>();
        builder.Services.AddTransient<ConfirmationPage>();
        builder.Services.AddSingleton<ITokenVault, SecureTokenVault>();
        builder.Services.AddSingleton<IIdentityClient, MsalIdentityClient>();
        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<ILocationDevice, MauiLocationDevice>();
        builder.Services.AddSingleton<LocationCoordinator>();
        builder.Services.AddSingleton(_ => new HttpClient(new ResilientCatalogHandler(TimeProvider.System) { InnerHandler = new DemoCatalogHandler() }));
        builder.Services.AddSingleton(provider => new CatalogClient(provider.GetRequiredService<HttpClient>(), new Uri("https://inventory.example/catalog")));
        builder.Services.AddSingleton<AppShell>();
        return builder.Build();
    }
}

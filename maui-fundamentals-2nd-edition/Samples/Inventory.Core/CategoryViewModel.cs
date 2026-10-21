using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace MauiInventory.Core;
public interface IExportSettings
{
    void SetLastExport(DateTimeOffset timestamp);
}

public partial class CategoryViewModel(ICategoryRepository repository, ITextFileStore files, IExportSettings settings) : ObservableObject
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public ObservableCollection<Category> Categories { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    private string newName = "";
    [ObservableProperty]
    private string newDescription = "";
    [ObservableProperty]
    private string errorMessage = "";
    [ObservableProperty]
    private DateTimeOffset? lastExportUtc;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    private bool isBusy;
    private bool CanRun() => !IsBusy;
    private bool CanAdd() => !IsBusy && !string.IsNullOrWhiteSpace(NewName);
    [RelayCommand(CanExecute = nameof(CanRun), IncludeCancelCommand = true)]
    private Task LoadAsync(CancellationToken token) => RunAsync(ReloadAsync, token);
    private async Task ReloadAsync(CancellationToken token)
    {
        var rows = await repository.ListAsync(token);
        token.ThrowIfCancellationRequested();
        Categories.Clear();
        foreach (var row in rows)
            Categories.Add(row);
    }

    [RelayCommand(CanExecute = nameof(CanAdd), IncludeCancelCommand = true)]
    private Task AddAsync(CancellationToken token) => RunAsync(async cancellation =>
    {
        await repository.AddAsync(new Category { Name = NewName, Description = NewDescription }, cancellation);
        NewName = "";
        NewDescription = "";
        await ReloadAsync(cancellation);
    }, token);
    [RelayCommand(CanExecute = nameof(CanRun), IncludeCancelCommand = true)]
    private Task ExportAsync(CancellationToken token) => RunAsync(async cancellation =>
    {
        var rows = await repository.ListAsync(cancellation);
        var timestamp = await CategoryExport.ExportAsync(rows, files, cancellation);
        settings.SetLastExport(timestamp);
        LastExportUtc = timestamp;
    }, token);
    private async Task RunAsync(Func<CancellationToken, Task> operation, CancellationToken token)
    {
        if (!await gate.WaitAsync(0, token))
            return;
        try
        {
            IsBusy = true;
            ErrorMessage = "";
            await operation(token);
        }
        catch (OperationCanceledException)when (token.IsCancellationRequested)
        {
            ErrorMessage = "Operation canceled.";
        }
        catch (Exception)
        {
            ErrorMessage = "Could not complete the category operation. Try again.";
        }
        finally
        {
            IsBusy = false;
            gate.Release();
        }
    }
}

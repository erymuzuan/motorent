# Dialog Pattern (MudBlazor)

Dialog patterns from rx-erp, adapted for MudBlazor.

## Pattern Guidelines

| Rule | Description |
|------|-------------|
| **Dialog Pattern** | Use for **SIMPLE** create/update operations |
| **Form Editor Pattern** | Use for **COMPLEX** create/update with multiple sections |
| **Clone Objects** | Always clone before passing to dialog (preserve original on cancel) |
| **Persistence in Parent** | Save in parent component, not in dialog |
| **No Business Logic** | Keep business logic in services, not in dialogs or parents |
| **Operation Name** | Use descriptive operation names for SubmitChanges |

## Base Dialog Component

```csharp
// Components/Dialogs/DialogBase.cs
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MotoRent.Server.Components.Dialogs;

public abstract class DialogBase<TItem> : ComponentBase where TItem : class
{
    [CascadingParameter]
    protected MudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public TItem? Item { get; set; }

    [Inject]
    protected RentalDataContext DataContext { get; set; } = default!;

    [Inject]
    protected ISnackbar Snackbar { get; set; } = default!;

    protected virtual bool OkDisabled => this.Item is null;

    protected void Ok()
    {
        this.MudDialog.Close(DialogResult.Ok(this.Item));
    }

    protected void Cancel()
    {
        this.MudDialog.Cancel();
    }
}
```

## Simple Edit Dialog

```razor
@* Components/Dialogs/MotorbikeDialog.razor *@
@inherits DialogBase<Motorbike>

<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">
            <MudIcon Icon="@Icons.Material.Filled.TwoWheeler" Class="mr-2" />
            @(Item?.MotorbikeId == 0 ? "Add Motorbike" : "Edit Motorbike")
        </MudText>
    </TitleContent>

    <DialogContent>
        @if (this.Item is not null)
        {
            <MudForm @ref="m_form" @bind-IsValid="m_isValid">
                <MudGrid>
                    <MudItem xs="12" sm="6">
                        <MudTextField @bind-Value="this.Item.LicensePlate"
                                      Label="License Plate"
                                      Required="true"
                                      RequiredError="License plate is required" />
                    </MudItem>
                    <MudItem xs="12" sm="6">
                        <MudSelect @bind-Value="this.Item.Brand" Label="Brand" Required="true">
                            <MudSelectItem Value="@("Honda")">Honda</MudSelectItem>
                            <MudSelectItem Value="@("Yamaha")">Yamaha</MudSelectItem>
                            <MudSelectItem Value="@("Suzuki")">Suzuki</MudSelectItem>
                        </MudSelect>
                    </MudItem>
                    <MudItem xs="12" sm="6">
                        <MudTextField @bind-Value="this.Item.Model" Label="Model" Required="true" />
                    </MudItem>
                    <MudItem xs="12" sm="6">
                        <MudNumericField @bind-Value="this.Item.EngineCC" Label="Engine CC" Min="50" Max="1000" />
                    </MudItem>
                    <MudItem xs="12" sm="6">
                        <MudNumericField @bind-Value="this.Item.DailyRate" Label="Daily Rate (THB)"
                                         Format="N2" Min="0" />
                    </MudItem>
                </MudGrid>
            </MudForm>
        }
    </DialogContent>

    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary" Disabled="@(!m_isValid)" OnClick="Ok">
            @(Item?.MotorbikeId == 0 ? "Add" : "Save")
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    private MudForm m_form = default!;
    private bool m_isValid;
}
```

## Calling Dialog from Parent

```csharp
// In parent component (e.g., MotorbikeList.razor.cs)
@inject IDialogService DialogService

private List<Motorbike> m_motorbikes = [];

private async Task EditMotorbike(Motorbike? motorbike = null)
{
    // Clone for new or existing
    var isNew = motorbike is null;
    motorbike = isNew ? new Motorbike { ShopId = this.ShopId } : motorbike.Clone();

    var parameters = new DialogParameters<MotorbikeDialog>
    {
        { x => x.Item, motorbike }
    };

    var options = new DialogOptions
    {
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        CloseOnEscapeKey = true
    };

    var dialog = await this.DialogService.ShowAsync<MotorbikeDialog>(
        isNew ? "Add Motorbike" : "Edit Motorbike",
        parameters,
        options);

    var result = await dialog.Result;

    if (result is { Canceled: false, Data: Motorbike item })
    {
        // Persistence in parent component
        using var session = this.DataContext.OpenSession();
        session.Attach(item);
        await session.SubmitChanges(isNew ? "Add" : "Edit");

        // Update list
        this.m_motorbikes.AddOrReplace(item, x => x.MotorbikeId == item.MotorbikeId);

        this.Snackbar.Add(isNew ? "Motorbike added" : "Motorbike updated", Severity.Success);
    }
}
```

## DialogHelper Service

```csharp
// Services/DialogHelper.cs
public class DialogHelper(IDialogService dialogService)
{
    private IDialogService DialogService { get; } = dialogService;

    /// <summary>
    /// Show confirmation dialog (Yes/No)
    /// </summary>
    public async Task<bool> ConfirmAsync(string message, string title = "Confirm")
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { x => x.ContentText, message },
            { x => x.ButtonText, "Yes" },
            { x => x.Color, Color.Primary }
        };

        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall };
        var dialog = await this.DialogService.ShowAsync<ConfirmDialog>(title, parameters, options);
        var result = await dialog.Result;

        return result is { Canceled: false };
    }

    /// <summary>
    /// Show delete confirmation (with warning color)
    /// </summary>
    public async Task<bool> ConfirmDeleteAsync(string itemName)
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { x => x.ContentText, $"Are you sure you want to delete '{itemName}'? This action cannot be undone." },
            { x => x.ButtonText, "Delete" },
            { x => x.Color, Color.Error }
        };

        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall };
        var dialog = await this.DialogService.ShowAsync<ConfirmDialog>("Confirm Delete", parameters, options);
        var result = await dialog.Result;

        return result is { Canceled: false };
    }

    /// <summary>
    /// Prompt for text input
    /// </summary>
    public async Task<string?> PromptAsync(string title, string message, string? defaultValue = null)
    {
        var parameters = new DialogParameters<PromptDialog>
        {
            { x => x.Message, message },
            { x => x.Value, defaultValue ?? "" }
        };

        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small };
        var dialog = await this.DialogService.ShowAsync<PromptDialog>(title, parameters, options);
        var result = await dialog.Result;

        return result is { Canceled: false, Data: string value } ? value : null;
    }
}
```

## Confirm Dialog Component

```razor
@* Components/Dialogs/ConfirmDialog.razor *@
<MudDialog>
    <DialogContent>
        <MudText>@ContentText</MudText>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="@Color" Variant="Variant.Filled" OnClick="Submit">@ButtonText</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public string ContentText { get; set; } = "Are you sure?";

    [Parameter]
    public string ButtonText { get; set; } = "OK";

    [Parameter]
    public Color Color { get; set; } = Color.Primary;

    private void Submit() => this.MudDialog.Close(DialogResult.Ok(true));
    private void Cancel() => this.MudDialog.Cancel();
}
```

## Usage Example

```csharp
@inject DialogHelper Dialog
@inject ISnackbar Snackbar

private async Task DeleteMotorbike(Motorbike bike)
{
    if (!await this.Dialog.ConfirmDeleteAsync(bike.LicensePlate))
        return;

    using var session = this.DataContext.OpenSession();
    session.Delete(bike);
    await session.SubmitChanges("Delete");

    this.m_motorbikes.Remove(bike);
    this.Snackbar.Add("Motorbike deleted", Severity.Success);
}
```

## Service Registration

```csharp
// Program.cs
builder.Services.AddScoped<DialogHelper>();
```

## Source
- Adapted from: `D:\project\work\rx-erp`
- MudBlazor Dialogs: https://mudblazor.com/components/dialog

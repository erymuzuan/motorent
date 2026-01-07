using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MotoRent.Client.Controls;

/// <summary>
/// Base class for MotoRent dialog components.
/// Provides common dialog functionality like closing and result handling.
/// </summary>
/// <typeparam name="TEntity">The entity type being edited in the dialog.</typeparam>
public abstract class MotoRentDialogBase<TEntity> : MotoRentComponentBase where TEntity : class, new()
{
    [CascadingParameter]
    protected MudDialogInstance? MudDialog { get; set; }

    /// <summary>
    /// The entity being edited or created.
    /// </summary>
    [Parameter]
    public TEntity Entity { get; set; } = new();

    /// <summary>
    /// Indicates whether this is a new entity being created.
    /// </summary>
    [Parameter]
    public bool IsNew { get; set; }

    /// <summary>
    /// Reference to the MudForm for validation.
    /// </summary>
    protected MudForm? Form { get; set; }

    /// <summary>
    /// Indicates whether the form is valid.
    /// </summary>
    protected bool FormValid { get; set; }

    /// <summary>
    /// Indicates whether a save operation is in progress.
    /// </summary>
    protected bool Saving { get; set; }

    /// <summary>
    /// Cancels the dialog without saving.
    /// </summary>
    protected virtual void Cancel()
    {
        MudDialog?.Cancel();
    }

    /// <summary>
    /// Closes the dialog with a successful result.
    /// </summary>
    protected virtual void Close()
    {
        MudDialog?.Close(DialogResult.Ok(Entity));
    }

    /// <summary>
    /// Closes the dialog with a custom result.
    /// </summary>
    protected virtual void Close(object result)
    {
        MudDialog?.Close(DialogResult.Ok(result));
    }

    /// <summary>
    /// Validates the form and returns true if valid.
    /// </summary>
    protected async Task<bool> ValidateFormAsync()
    {
        if (Form == null) return false;

        await Form.Validate();
        return FormValid;
    }

    /// <summary>
    /// Gets the save button text based on IsNew.
    /// </summary>
    protected string SaveButtonText => IsNew ? "Add" : "Save";
}

/// <summary>
/// Localized version of the dialog base class.
/// </summary>
/// <typeparam name="TEntity">The entity type being edited.</typeparam>
/// <typeparam name="TLocalizer">The component type for localization.</typeparam>
public abstract class LocalizedDialogBase<TEntity, TLocalizer> : MotoRentDialogBase<TEntity>
    where TEntity : class, new()
{
    [Inject] protected Microsoft.Extensions.Localization.IStringLocalizer<TLocalizer> Localizer { get; set; } = null!;

    /// <summary>
    /// Gets a localized string, returning the default text if the key is not found.
    /// </summary>
    protected string GetLocalizedText(string key, string defaultText = "")
    {
        var localized = Localizer[key].Value;
        if (localized.Equals(key, StringComparison.InvariantCultureIgnoreCase))
            return string.IsNullOrEmpty(defaultText) ? key : defaultText;
        return localized;
    }
}

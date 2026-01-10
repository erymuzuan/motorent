using Microsoft.AspNetCore.Components;
using MotoRent.Client.Services;

namespace MotoRent.Client.Controls;

/// <summary>
/// Base class for MotoRent dialog components.
/// Provides common dialog functionality like closing and result handling.
/// </summary>
/// <typeparam name="TEntity">The entity type being edited in the dialog.</typeparam>
public abstract class MotoRentDialogBase<TEntity> : MotoRentComponentBase where TEntity : class, new()
{
    [Inject]
    protected IModalService ModalService { get; set; } = null!;

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
    /// Indicates whether the form is valid.
    /// </summary>
    protected bool FormValid { get; set; }

    /// <summary>
    /// Indicates whether a save operation is in progress.
    /// </summary>
    protected bool Saving { get; set; }

    /// <summary>
    /// Gets the form ID for the modal footer's form attribute.
    /// </summary>
    protected virtual string FormId => this.GetType().Name.ToLowerInvariant().Replace("`1", "");

    /// <summary>
    /// Cancels the dialog without saving.
    /// </summary>
    protected virtual void Cancel()
    {
        this.ModalService.Close(ModalResult.Cancel());
    }

    /// <summary>
    /// Closes the dialog with a successful result.
    /// </summary>
    protected virtual void Close()
    {
        this.ModalService.Close(ModalResult.Ok(this.Entity));
    }

    /// <summary>
    /// Closes the dialog with a custom result.
    /// </summary>
    protected virtual void Close(object result)
    {
        this.ModalService.Close(ModalResult.Ok(result));
    }

    /// <summary>
    /// Gets the save button text based on IsNew.
    /// </summary>
    protected string SaveButtonText => this.IsNew ? "Add" : "Save";

    /// <summary>
    /// Gets whether the OK button should be disabled.
    /// Override in derived classes to implement custom validation.
    /// </summary>
    protected virtual bool OkDisabled => !this.FormValid;
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
        var localized = this.Localizer[key].Value;
        if (localized.Equals(key, StringComparison.InvariantCultureIgnoreCase))
            return string.IsNullOrEmpty(defaultText) ? key : defaultText;
        return localized;
    }
}

/// <summary>
/// Alias for MotoRentDialogBase for easier transition.
/// </summary>
public abstract class MotoRentModalBase<TEntity> : MotoRentDialogBase<TEntity>
    where TEntity : class, new()
{
    /// <summary>
    /// The item being edited (alias for Entity).
    /// </summary>
    [Parameter]
    public TEntity? Item
    {
        get => this.Entity;
        set => this.Entity = value ?? new TEntity();
    }

    /// <summary>
    /// Gets the result output. Override to customize.
    /// </summary>
    protected virtual object? ResultOutput => this.Entity;

    /// <summary>
    /// Handles OK button click.
    /// </summary>
    protected virtual Task OkClick(TEntity? output)
    {
        this.ModalService.Close(ModalResult.Ok(this.ResultOutput ?? output));
        return Task.CompletedTask;
    }
}

/// <summary>
/// Localized version of the modal base class matching rx-erp pattern.
/// </summary>
public abstract class LocalizedModalBase<TEntity, TLocalizer> : MotoRentModalBase<TEntity>
    where TEntity : class, new()
{
    [Inject] protected Microsoft.Extensions.Localization.IStringLocalizer<TLocalizer> Localizer { get; set; } = null!;
}

using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MotoRent.Client.Controls;

/// <summary>
/// Extension methods for IDialogService to enable fluent dialog creation.
/// </summary>
public static class DialogServiceExtensions
{
    /// <summary>
    /// Creates a fluent dialog builder for the specified component type.
    /// </summary>
    /// <typeparam name="T">The dialog component type</typeparam>
    /// <param name="dialogService">The dialog service</param>
    /// <param name="title">The dialog title</param>
    /// <returns>A fluent builder for configuring and showing the dialog</returns>
    /// <example>
    /// <code>
    /// var result = await DialogService
    ///     .CreateDialog&lt;MotorbikeDialog&gt;("Add Motorbike")
    ///     .WithParameter(x => x.Entity, new Motorbike())
    ///     .WithParameter(x => x.IsNew, true)
    ///     .Large()
    ///     .ShowAsync();
    /// </code>
    /// </example>
    public static DialogFluent<T> CreateDialog<T>(this IDialogService dialogService, string title)
        where T : IComponent
    {
        return new DialogFluent<T>(dialogService, title);
    }

    /// <summary>
    /// Creates a small dialog (MaxWidth.Small).
    /// </summary>
    public static DialogFluent<T> CreateSmallDialog<T>(this IDialogService dialogService, string title)
        where T : IComponent
    {
        return new DialogFluent<T>(dialogService, title).Small();
    }

    /// <summary>
    /// Creates a large dialog (MaxWidth.Large).
    /// </summary>
    public static DialogFluent<T> CreateLargeDialog<T>(this IDialogService dialogService, string title)
        where T : IComponent
    {
        return new DialogFluent<T>(dialogService, title).Large();
    }

    /// <summary>
    /// Creates a fullscreen dialog.
    /// </summary>
    public static DialogFluent<T> CreateFullscreenDialog<T>(this IDialogService dialogService, string title)
        where T : IComponent
    {
        return new DialogFluent<T>(dialogService, title).Fullscreen();
    }
}

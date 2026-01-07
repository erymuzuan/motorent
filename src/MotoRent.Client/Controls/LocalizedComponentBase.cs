using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace MotoRent.Client.Controls;

/// <summary>
/// Base class for localized MotoRent Blazor components.
/// Provides type-safe access to component-specific resource files.
/// </summary>
/// <typeparam name="T">The component type for localization resource lookup.</typeparam>
public class LocalizedComponentBase<T> : MotoRentComponentBase
{
    [Inject] protected IStringLocalizer<T> Localizer { get; set; } = null!;

    /// <summary>
    /// Gets a localized string, returning the default text if the key is not found.
    /// </summary>
    /// <param name="key">The resource key to look up.</param>
    /// <param name="defaultText">The default text to return if the key is not found.</param>
    /// <returns>The localized string or the default text.</returns>
    protected string GetLocalizedText(string key, string defaultText = "")
    {
        var localized = Localizer[key].Value;

        // If the localized value equals the key, the translation wasn't found
        if (localized.Equals(key, StringComparison.InvariantCultureIgnoreCase))
            return string.IsNullOrEmpty(defaultText) ? key : defaultText;

        return localized;
    }

    /// <summary>
    /// Gets a localized string with format arguments.
    /// </summary>
    /// <param name="key">The resource key to look up.</param>
    /// <param name="arguments">The format arguments.</param>
    /// <returns>The formatted localized string.</returns>
    protected string GetLocalizedText(string key, params object[] arguments)
    {
        return Localizer[key, arguments].Value;
    }
}

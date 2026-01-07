using MudBlazor;

namespace MotoRent.Client;

public static class MotoRentTheme
{
    public static MudTheme Theme { get; } = new MudTheme
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#00897B",        // Teal 600
            PrimaryDarken = "#00695C",  // Teal 800
            PrimaryLighten = "#4DB6AC", // Teal 300
            Secondary = "#FF7043",      // Deep Orange accent
            SecondaryDarken = "#E64A19",
            SecondaryLighten = "#FFAB91",
            AppbarBackground = "#00897B",
            AppbarText = "#FFFFFF",
            Background = "#FAFAFA",
            Surface = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "rgba(0,0,0,0.87)",
            Success = "#4CAF50",
            Warning = "#FF9800",
            Error = "#F44336",
            Info = "#2196F3",
            TextPrimary = "rgba(0,0,0,0.87)",
            TextSecondary = "rgba(0,0,0,0.6)",
            ActionDefault = "#757575",
            ActionDisabled = "rgba(0,0,0,0.26)",
            ActionDisabledBackground = "rgba(0,0,0,0.12)",
            Divider = "rgba(0,0,0,0.12)",
            DividerLight = "rgba(0,0,0,0.06)",
            TableLines = "rgba(0,0,0,0.12)",
            LinesDefault = "rgba(0,0,0,0.12)",
            LinesInputs = "rgba(0,0,0,0.42)",
            TextDisabled = "rgba(0,0,0,0.38)"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#4DB6AC",        // Teal 300
            PrimaryDarken = "#00897B",  // Teal 600
            PrimaryLighten = "#80CBC4", // Teal 200
            Secondary = "#FFAB91",      // Deep Orange 200
            SecondaryDarken = "#FF7043",
            SecondaryLighten = "#FFCCBC",
            AppbarBackground = "#1E1E1E",
            AppbarText = "#FFFFFF",
            Background = "#121212",
            Surface = "#1E1E1E",
            DrawerBackground = "#1E1E1E",
            DrawerText = "rgba(255,255,255,0.87)",
            Success = "#81C784",
            Warning = "#FFB74D",
            Error = "#E57373",
            Info = "#64B5F6",
            TextPrimary = "rgba(255,255,255,0.87)",
            TextSecondary = "rgba(255,255,255,0.6)",
            ActionDefault = "#BDBDBD",
            ActionDisabled = "rgba(255,255,255,0.26)",
            ActionDisabledBackground = "rgba(255,255,255,0.12)",
            Divider = "rgba(255,255,255,0.12)",
            DividerLight = "rgba(255,255,255,0.06)",
            TableLines = "rgba(255,255,255,0.12)",
            LinesDefault = "rgba(255,255,255,0.12)",
            LinesInputs = "rgba(255,255,255,0.42)",
            TextDisabled = "rgba(255,255,255,0.38)"
        },
        Typography = new Typography
        {
            Default = new Default
            {
                FontFamily = ["Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = ".875rem",
                FontWeight = 400,
                LineHeight = 1.43,
                LetterSpacing = ".01071em"
            },
            H1 = new H1
            {
                FontFamily = ["Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = "6rem",
                FontWeight = 300,
                LineHeight = 1.167,
                LetterSpacing = "-.01562em"
            },
            H2 = new H2
            {
                FontFamily = ["Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = "3.75rem",
                FontWeight = 300,
                LineHeight = 1.2,
                LetterSpacing = "-.00833em"
            },
            H3 = new H3
            {
                FontFamily = ["Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = "3rem",
                FontWeight = 400,
                LineHeight = 1.167,
                LetterSpacing = "0"
            },
            H4 = new H4
            {
                FontFamily = ["Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = "2.125rem",
                FontWeight = 400,
                LineHeight = 1.235,
                LetterSpacing = ".00735em"
            },
            H5 = new H5
            {
                FontFamily = ["Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = "1.5rem",
                FontWeight = 400,
                LineHeight = 1.334,
                LetterSpacing = "0"
            },
            H6 = new H6
            {
                FontFamily = ["Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = "1.25rem",
                FontWeight = 500,
                LineHeight = 1.6,
                LetterSpacing = ".0075em"
            }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "4px",
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "260px"
        }
    };
}

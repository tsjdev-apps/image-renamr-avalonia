using System.Globalization;
using ImageRenamr.App.Resources.Localization;

namespace ImageRenamr.Tests;

/// <summary>
/// Verifies resource availability and normal .NET culture fallback behavior.
/// </summary>
public sealed class LocalizationTests
{
    [Fact]
    public void EnglishResourcesAndSingularPluralProgressAreAvailable()
    {
        using CultureScope scope = new("en-US");

        Assert.Equal("Source folder", Strings.InputFolder_Label);
        Assert.Equal("1 / 1 file processed", Strings.FormatProgress(1, 1));
        Assert.Equal("2 / 3 files processed", Strings.FormatProgress(2, 3));
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("de-AT")]
    [InlineData("de-CH")]
    public void GermanResourcesAndSingularPluralProgressAreAvailable(string cultureName)
    {
        using CultureScope scope = new(cultureName);

        Assert.Equal("Quellordner", Strings.InputFolder_Label);
        Assert.Equal("1 / 1 Datei verarbeitet", Strings.FormatProgress(1, 1));
        Assert.Equal("2 / 3 Dateien verarbeitet", Strings.FormatProgress(2, 3));
    }

    [Fact]
    public void UnsupportedCultureFallsBackToEnglish()
    {
        using CultureScope scope = new("fr-FR");

        Assert.Equal("Source folder", Strings.InputFolder_Label);
        Assert.Equal("en-US", Strings.ThemeLocale);
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo previousCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string cultureName)
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}

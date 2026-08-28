using System.Globalization;
using System.Reflection;
using Bnp.Localization;

namespace Bnp.Tests.Localization;

public sealed class EditorCopyCatalogTests
{
    [Theory]
    [InlineData("en-US", "Documents")]
    [InlineData("pt-BR", "Documentos")]
    [InlineData("es-ES", "Documentos")]
    [InlineData("fr-FR", "Documents")]
    public void LoadReturnsCompleteCopyForSupportedLanguage(string cultureName, string expectedDocuments)
    {
        var copy = EditorCopyCatalog.Load(CultureInfo.GetCultureInfo(cultureName));

        Assert.Equal(expectedDocuments, copy.Documents);
        Assert.All(
            typeof(EditorCopy).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.PropertyType == typeof(string)),
            property => Assert.False(string.IsNullOrWhiteSpace((string?)property.GetValue(copy))));
        Assert.Equal(8, copy.TextColors.Count);
        Assert.Equal(10, copy.DocumentColors.Count);
        Assert.Equal(70, copy.DocumentIcons.Count);

        var english = EditorCopyCatalog.Load(CultureInfo.GetCultureInfo("en-US"));
        Assert.Equal(
            english.TextColors.Keys.Order(StringComparer.Ordinal),
            copy.TextColors.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(
            english.DocumentColors.Keys.Order(StringComparer.Ordinal),
            copy.DocumentColors.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(
            english.DocumentIcons.Keys.Order(StringComparer.Ordinal),
            copy.DocumentIcons.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void LoadFallsBackToEnglishForUnsupportedLanguage()
    {
        var copy = EditorCopyCatalog.Load(CultureInfo.GetCultureInfo("de-DE"));

        Assert.Equal("Documents", copy.Documents);
    }
}
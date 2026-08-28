using Avalonia.Media;
using AvaloniaRichEditor.Controls;
using AvaloniaRichEditor.Documents;

namespace Bnp.Presentation;

internal static class RichEditorThemeApplicator
{
    private const double DefaultParagraphMarginBottom = 10;
    private const double CurrentParagraphMarginBottom = 6;
    private const double LegacyParagraphMarginBottom = 1;

    public static void Apply(RichEditor editor, IBrush automaticTextBrush)
    {
        if (editor.Document is not { } document)
        {
            return;
        }

        foreach (var block in document.Blocks)
        {
            ApplyBlock(block, automaticTextBrush);
        }

        editor.InvalidateVisual();
    }

    public static void NormalizeParagraphSpacing(RichEditor editor)
    {
        if (editor.Document is not { } document)
        {
            return;
        }

        var spacingChanged = false;
        foreach (var block in document.Blocks)
        {
            spacingChanged |= NormalizeBlockSpacing(block);
        }

        if (spacingChanged)
        {
            editor.InvalidateVisual();
        }
    }

    public static bool UsesAutomaticTextColor(IBrush? brush)
    {
        if (brush is not SolidColorBrush solidColorBrush)
        {
            return brush is null;
        }

        return solidColorBrush.Color == Color.Parse("#202020") ||
            solidColorBrush.Color == Color.Parse("#F6F5F4") ||
            solidColorBrush.Color == Colors.Black ||
            solidColorBrush.Color == Colors.White;
    }

    private static void ApplyBlock(Block block, IBrush automaticTextBrush)
    {
        if (block is Paragraph paragraph)
        {
            NormalizeParagraphSpacing(paragraph);

            foreach (var inline in paragraph.Inlines)
            {
                if (inline is AvaloniaRichEditor.Documents.Run run)
                {
                    run.FontFamily = null;
                    run.FontSize = 0;
                    if (UsesAutomaticTextColor(run.Foreground))
                    {
                        run.Foreground = automaticTextBrush;
                    }
                }
                else if (inline is InlineTable inlineTable)
                {
                    ApplyTable(inlineTable.Table, automaticTextBrush);
                }
            }
        }
        else if (block is TableBlock table)
        {
            ApplyTable(table, automaticTextBrush);
        }
    }

    private static bool NormalizeBlockSpacing(Block block)
    {
        if (block is Paragraph paragraph)
        {
            return NormalizeParagraphSpacing(paragraph);
        }

        if (block is not TableBlock table)
        {
            return false;
        }

        var spacingChanged = false;
        foreach (var row in table.Cells)
        {
            foreach (var cell in row)
            {
                foreach (var cellBlock in cell.Blocks)
                {
                    spacingChanged |= NormalizeBlockSpacing(cellBlock);
                }
            }
        }

        return spacingChanged;
    }

    private static bool NormalizeParagraphSpacing(Paragraph paragraph)
    {
        if (Math.Abs(paragraph.MarginBottom - DefaultParagraphMarginBottom) >= 0.01 &&
            Math.Abs(paragraph.MarginBottom - CurrentParagraphMarginBottom) >= 0.01 &&
            Math.Abs(paragraph.MarginBottom - LegacyParagraphMarginBottom) >= 0.01)
        {
            return false;
        }

        paragraph.MarginBottom = 0;
        return true;
    }

    private static void ApplyTable(TableBlock table, IBrush automaticTextBrush)
    {
        foreach (var row in table.Cells)
        {
            foreach (var cell in row)
            {
                foreach (var block in cell.Blocks)
                {
                    ApplyBlock(block, automaticTextBrush);
                }
            }
        }
    }
}
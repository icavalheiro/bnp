using Avalonia.Media;
using AvaloniaRichEditor.Controls;
using AvaloniaRichEditor.Documents;

namespace Bnp.Presentation;

internal static class RichEditorThemeApplicator
{
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
            if (Math.Abs(paragraph.MarginBottom - 10) < 0.01)
            {
                paragraph.MarginBottom = 1;
            }

            foreach (var inline in paragraph.Inlines)
            {
                if (inline is AvaloniaRichEditor.Documents.Run run &&
                    UsesAutomaticTextColor(run.Foreground))
                {
                    run.Foreground = automaticTextBrush;
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
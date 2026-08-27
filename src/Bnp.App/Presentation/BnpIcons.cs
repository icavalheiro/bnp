using AvaloniaRichEditor.Controls;
using Lucide.Avalonia;

namespace Bnp.Presentation;

internal static class BnpIcons
{
    public static LucideIcon Create(LucideIconKind kind, double size = 18)
    {
        return new LucideIcon
        {
            Kind = kind,
            Size = size,
            StrokeWidth = 1.8
        };
    }

    public static LucideIcon CreateDocumentIcon(string iconKey, double size = 18)
    {
        return Create(iconKey switch
        {
            "idea" => LucideIconKind.Lightbulb,
            "notebook" => LucideIconKind.NotebookTabs,
            "favorite" => LucideIconKind.Star,
            "todo" => LucideIconKind.ListChecks,
            _ => LucideIconKind.FileText
        }, size);
    }

    public static void ConfigureRichEditorIcons()
    {
        RichEditorIcons.Provider = icon => icon switch
        {
            RichEditorIcon.Bold => Create(LucideIconKind.Bold, 16),
            RichEditorIcon.Italic => Create(LucideIconKind.Italic, 16),
            RichEditorIcon.Underline => Create(LucideIconKind.Underline, 16),
            RichEditorIcon.Strikethrough => Create(LucideIconKind.Strikethrough, 16),
            RichEditorIcon.Highlight => Create(LucideIconKind.Highlighter, 16),
            RichEditorIcon.AlignLeft => Create(LucideIconKind.TextAlignStart, 16),
            RichEditorIcon.AlignCenter => Create(LucideIconKind.TextAlignCenter, 16),
            RichEditorIcon.AlignRight => Create(LucideIconKind.TextAlignEnd, 16),
            RichEditorIcon.Undo => Create(LucideIconKind.Undo2, 16),
            RichEditorIcon.Redo => Create(LucideIconKind.Redo2, 16),
            _ => null
        };
    }
}
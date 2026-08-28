using Avalonia.Media;
using AvaloniaRichEditor.Controls;
using Lucide.Avalonia;

namespace Bnp.Presentation;

internal static class BnpIcons
{
    public static IReadOnlyList<DocumentIconOption> DocumentIcons { get; } =
    [
        new("file-text", LucideIconKind.FileText),
        new("notebook", LucideIconKind.NotebookTabs),
        new("idea", LucideIconKind.Lightbulb),
        new("favorite", LucideIconKind.Star),
        new("todo", LucideIconKind.ListChecks),
        new("book-open", LucideIconKind.BookOpen),
        new("bookmark", LucideIconKind.Bookmark),
        new("briefcase", LucideIconKind.Briefcase),
        new("calendar", LucideIconKind.CalendarDays),
        new("camera", LucideIconKind.Camera),
        new("chart", LucideIconKind.ChartBar),
        new("check", LucideIconKind.CircleCheck),
        new("clock", LucideIconKind.Clock),
        new("cloud", LucideIconKind.Cloud),
        new("code", LucideIconKind.Code),
        new("coffee", LucideIconKind.Coffee),
        new("compass", LucideIconKind.Compass),
        new("database", LucideIconKind.Database),
        new("flag", LucideIconKind.Flag),
        new("folder", LucideIconKind.Folder),
        new("gift", LucideIconKind.Gift),
        new("globe", LucideIconKind.Globe),
        new("graduation", LucideIconKind.GraduationCap),
        new("heart", LucideIconKind.Heart),
        new("home", LucideIconKind.House),
        new("image", LucideIconKind.Image),
        new("inbox", LucideIconKind.Inbox),
        new("key", LucideIconKind.KeyRound),
        new("landmark", LucideIconKind.Landmark),
        new("languages", LucideIconKind.Languages),
        new("layers", LucideIconKind.Layers),
        new("link", LucideIconKind.Link),
        new("lock", LucideIconKind.LockKeyhole),
        new("mail", LucideIconKind.Mail),
        new("map", LucideIconKind.Map),
        new("message", LucideIconKind.MessageCircle),
        new("monitor", LucideIconKind.Monitor),
        new("moon", LucideIconKind.Moon),
        new("music", LucideIconKind.Music),
        new("package", LucideIconKind.Package),
        new("palette", LucideIconKind.Palette),
        new("pen", LucideIconKind.PenLine),
        new("phone", LucideIconKind.Phone),
        new("pin", LucideIconKind.MapPin),
        new("plane", LucideIconKind.Plane),
        new("rocket", LucideIconKind.Rocket),
        new("search", LucideIconKind.Search),
        new("settings", LucideIconKind.Settings),
        new("shield", LucideIconKind.Shield),
        new("shopping", LucideIconKind.ShoppingCart),
        new("sparkles", LucideIconKind.Sparkles),
        new("sun", LucideIconKind.Sun),
        new("tag", LucideIconKind.Tag),
        new("target", LucideIconKind.Target),
        new("terminal", LucideIconKind.Terminal),
        new("trophy", LucideIconKind.Trophy),
        new("user", LucideIconKind.User),
        new("users", LucideIconKind.Users),
        new("video", LucideIconKind.Video),
        new("wallet", LucideIconKind.Wallet),
        new("wrench", LucideIconKind.Wrench),
        new("bell", LucideIconKind.Bell),
        new("calculator", LucideIconKind.Calculator),
        new("clipboard", LucideIconKind.Clipboard),
        new("cpu", LucideIconKind.Cpu),
        new("crown", LucideIconKind.Crown),
        new("dumbbell", LucideIconKind.Dumbbell),
        new("eye", LucideIconKind.Eye),
        new("gamepad", LucideIconKind.Gamepad2),
        new("headphones", LucideIconKind.Headphones)
    ];

    public static LucideIcon Create(LucideIconKind kind, double size = 18)
    {
        return new LucideIcon
        {
            Kind = kind,
            Size = size,
            StrokeWidth = 1.8
        };
    }

    public static LucideIcon CreateDocumentIcon(
        string iconKey,
        string colorKey = "#5B6B82",
        double size = 18)
    {
        var option = DocumentIcons.FirstOrDefault(option => option.Key == iconKey) ?? DocumentIcons[0];
        var icon = Create(option.Kind, size);
        icon.Foreground = new SolidColorBrush(Color.Parse(colorKey));
        return icon;
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

internal sealed record DocumentIconOption(string Key, LucideIconKind Kind);
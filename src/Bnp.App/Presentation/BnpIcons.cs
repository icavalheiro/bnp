using Avalonia.Media;
using AvaloniaRichEditor.Controls;
using Lucide.Avalonia;

namespace Bnp.Presentation;

internal static class BnpIcons
{
    public static IReadOnlyList<DocumentIconOption> DocumentIcons { get; } =
    [
        new("file-text", "Document", LucideIconKind.FileText),
        new("notebook", "Notebook", LucideIconKind.NotebookTabs),
        new("idea", "Idea", LucideIconKind.Lightbulb),
        new("favorite", "Favorite", LucideIconKind.Star),
        new("todo", "To-do", LucideIconKind.ListChecks),
        new("book-open", "Book", LucideIconKind.BookOpen),
        new("bookmark", "Bookmark", LucideIconKind.Bookmark),
        new("briefcase", "Work", LucideIconKind.Briefcase),
        new("calendar", "Calendar", LucideIconKind.CalendarDays),
        new("camera", "Camera", LucideIconKind.Camera),
        new("chart", "Chart", LucideIconKind.ChartBar),
        new("check", "Complete", LucideIconKind.CircleCheck),
        new("clock", "Clock", LucideIconKind.Clock),
        new("cloud", "Cloud", LucideIconKind.Cloud),
        new("code", "Code", LucideIconKind.Code),
        new("coffee", "Coffee", LucideIconKind.Coffee),
        new("compass", "Compass", LucideIconKind.Compass),
        new("database", "Database", LucideIconKind.Database),
        new("flag", "Flag", LucideIconKind.Flag),
        new("folder", "Folder", LucideIconKind.Folder),
        new("gift", "Gift", LucideIconKind.Gift),
        new("globe", "Globe", LucideIconKind.Globe),
        new("graduation", "Study", LucideIconKind.GraduationCap),
        new("heart", "Heart", LucideIconKind.Heart),
        new("home", "Home", LucideIconKind.House),
        new("image", "Image", LucideIconKind.Image),
        new("inbox", "Inbox", LucideIconKind.Inbox),
        new("key", "Key", LucideIconKind.KeyRound),
        new("landmark", "Landmark", LucideIconKind.Landmark),
        new("languages", "Languages", LucideIconKind.Languages),
        new("layers", "Layers", LucideIconKind.Layers),
        new("link", "Link", LucideIconKind.Link),
        new("lock", "Private", LucideIconKind.LockKeyhole),
        new("mail", "Mail", LucideIconKind.Mail),
        new("map", "Map", LucideIconKind.Map),
        new("message", "Message", LucideIconKind.MessageCircle),
        new("monitor", "Monitor", LucideIconKind.Monitor),
        new("moon", "Moon", LucideIconKind.Moon),
        new("music", "Music", LucideIconKind.Music),
        new("package", "Package", LucideIconKind.Package),
        new("palette", "Palette", LucideIconKind.Palette),
        new("pen", "Writing", LucideIconKind.PenLine),
        new("phone", "Phone", LucideIconKind.Phone),
        new("pin", "Location", LucideIconKind.MapPin),
        new("plane", "Travel", LucideIconKind.Plane),
        new("rocket", "Launch", LucideIconKind.Rocket),
        new("search", "Research", LucideIconKind.Search),
        new("settings", "Settings", LucideIconKind.Settings),
        new("shield", "Security", LucideIconKind.Shield),
        new("shopping", "Shopping", LucideIconKind.ShoppingCart),
        new("sparkles", "Sparkles", LucideIconKind.Sparkles),
        new("sun", "Sun", LucideIconKind.Sun),
        new("tag", "Tag", LucideIconKind.Tag),
        new("target", "Target", LucideIconKind.Target),
        new("terminal", "Terminal", LucideIconKind.Terminal),
        new("trophy", "Achievement", LucideIconKind.Trophy),
        new("user", "Person", LucideIconKind.User),
        new("users", "Team", LucideIconKind.Users),
        new("video", "Video", LucideIconKind.Video),
        new("wallet", "Finance", LucideIconKind.Wallet),
        new("wrench", "Tools", LucideIconKind.Wrench),
        new("bell", "Reminder", LucideIconKind.Bell),
        new("calculator", "Calculator", LucideIconKind.Calculator),
        new("clipboard", "Clipboard", LucideIconKind.Clipboard),
        new("cpu", "Technology", LucideIconKind.Cpu),
        new("crown", "Crown", LucideIconKind.Crown),
        new("dumbbell", "Fitness", LucideIconKind.Dumbbell),
        new("eye", "Review", LucideIconKind.Eye),
        new("gamepad", "Game", LucideIconKind.Gamepad2),
        new("headphones", "Audio", LucideIconKind.Headphones)
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

internal sealed record DocumentIconOption(string Key, string Label, LucideIconKind Kind);
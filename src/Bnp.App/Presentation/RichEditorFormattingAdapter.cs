using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia.Media;
using AvaloniaRichEditor.Controls;
using AvaloniaRichEditor.Documents;

namespace Bnp.Presentation;

internal static class RichEditorFormattingAdapter
{
    private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;

    private static readonly FieldInfo SelectionStartField =
        typeof(RichEditor).GetField("_selectionStart", InstanceNonPublic)
        ?? throw new MissingFieldException(typeof(RichEditor).FullName, "_selectionStart");

    private static readonly FieldInfo SelectionEndField =
        typeof(RichEditor).GetField("_selectionEnd", InstanceNonPublic)
        ?? throw new MissingFieldException(typeof(RichEditor).FullName, "_selectionEnd");

    private static readonly MethodInfo ApplyStyleToSelectionMethod =
        typeof(RichEditor).GetMethod("ApplyStyleToSelection", InstanceNonPublic)
        ?? throw new MissingMethodException(typeof(RichEditor).FullName, "ApplyStyleToSelection");

    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicFields, typeof(RichEditor))]
    public static bool HasSelection(RichEditor editor)
    {
        var start = SelectionStartField.GetValue(editor) as TextPointer;
        var end = SelectionEndField.GetValue(editor) as TextPointer;
        return start?.Paragraph is not null && end?.Paragraph is not null && start.CompareTo(end) != 0;
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, typeof(RichEditor))]
    public static void ClearFormatting(RichEditor editor, IBrush automaticForeground)
    {
        Action<Run> clearStyle = run =>
        {
            run.FontWeight = FontWeight.Normal;
            run.FontStyle = FontStyle.Normal;
            run.Foreground = automaticForeground;
            run.Background = null;
            run.TextDecorations = null;
            run.NavigateUri = null;
        };
        ApplyStyleToSelectionMethod.Invoke(editor, [clearStyle]);
    }
}
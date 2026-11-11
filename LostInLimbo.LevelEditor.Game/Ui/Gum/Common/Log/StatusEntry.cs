namespace LostInLimbo.LevelEditor.Ui.Gum.Common.Log;

/// <summary>
/// One line of a status log.
/// </summary>
/// <param name="Text">Line text.</param>
/// <param name="Severity">Severity.</param>
public sealed record StatusEntry(string Text, EnStatusSeverity Severity);

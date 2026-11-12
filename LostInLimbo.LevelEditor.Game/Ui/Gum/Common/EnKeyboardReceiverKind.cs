namespace LostInLimbo.LevelEditor.Ui.Gum;

/// <summary>
/// What kind of Gum control currently receives keyboard input
/// (<c>InteractiveGue.CurrentInputReceiver</c>), as far as gameplay keyboard capture cares.
/// </summary>
public enum EnKeyboardReceiverKind
{
  /// <summary>No Gum control receives keyboard input.</summary>
  None,

  /// <summary>A text box (typing).</summary>
  TextInput,

  /// <summary>A visible list navigated with arrow keys, for example an open ComboBox dropdown.</summary>
  ListNavigation,

  /// <summary>Any other receiver; does not need the keyboard exclusively.</summary>
  Other,
}

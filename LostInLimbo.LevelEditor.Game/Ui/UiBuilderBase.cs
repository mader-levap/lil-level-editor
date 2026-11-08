using LostInLimbo.LevelEditor.Ui.Main;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.Sprites;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace LostInLimbo.LevelEditor.Ui;

/// <summary>
/// Base class for all UI builders.
/// </summary>
/// <remarks>
/// Currently, provides only logging.
/// </remarks>
public abstract class UiBuilderBase
{
  #region Logger

  /// <summary>Internal logger instance.</summary>
  private Logger? _logger;

  /// <summary>
  /// Logger for your UI builders. Use in same way as any other <c>Log</c> elsewhere.
  /// </summary>
  protected Logger Log
  {
    get
    {
      if (_logger != null) return _logger;

      // Lazily create logger.
      var className = GetType().FullName;
      _logger = GlobalLogger.GetLogger(className!);
      return _logger;
    }
  }

  #endregion

  #region Basic methods.

  /// <summary>Build panel.</summary>
  /// <returns>Panel.</returns>
  abstract public Panel Build();

  /// <summary>Call update.</summary>
  virtual public void Update()
  {
    // NO-OP
  }

  #endregion

  #region UI component helpers.

  /// <summary>
  /// Create new button with icon.
  /// </summary>
  /// <returns>Button.</returns>
  protected static Button CreateImgButton(string name, string imageUrl)
  {
    Button button = new()
    {
      Name = name,
      Width = 48,
      Height = 48,
      Margin = new Thickness(0, 2, 0, 2),
      HorizontalAlignment = HorizontalAlignment.Center,
      VerticalAlignment = VerticalAlignment.Center,
      PressedImage = new SpriteFromSheet
      {
        Sheet = UiRes.SpriteBtnUnselected,
        CurrentFrame = 2,
      },
      NotPressedImage = new SpriteFromSheet
      {
        Sheet = UiRes.SpriteBtnUnselected,
        CurrentFrame = 1,
      },
      MouseOverImage = new SpriteFromSheet
      {
        Sheet = UiRes.SpriteBtnUnselected,
        CurrentFrame = 0,
      },
      Content = CreateImage(imageUrl),
    };
    return button;
  }

  /// <summary>
  /// Create image for given URL.
  /// </summary>
  /// <param name="imageUrl">URL of image.</param>
  /// <returns></returns>
  protected static ImageElement CreateImage(string imageUrl)
  {
    return new ImageElement()
    {
      Source = new SpriteFromTexture
      {
        Texture = UiRes.Script.Content.Load<Texture>(imageUrl)
      },
    };
  }

  /// <summary>
  /// Set enable state of button with feedback for user.
  /// </summary>
  /// <param name="button">Button.</param>
  /// <param name="enabled">True if button should be enabled, otherwise false.</param>
  protected static void EnableButton(Button button, bool enabled)
  {
    button.IsEnabled = enabled;
    if (enabled)
    {
      if (button.Content is ImageElement imageElement)
      {
        imageElement.Color = new Color(255, 255, 255);
      } else if (button.Content is TextBlock textBlock)
      {
        textBlock.TextColor = new Color(255, 255, 255);
      }
    } else
    {
      if (button.Content is ImageElement imageElement)
      {
        imageElement.Color = new Color(192, 192, 192);
      } else if (button.Content is TextBlock textBlock)
      {
        textBlock.TextColor = new Color(192, 192, 192);
      }
    }
  }

  #endregion
}

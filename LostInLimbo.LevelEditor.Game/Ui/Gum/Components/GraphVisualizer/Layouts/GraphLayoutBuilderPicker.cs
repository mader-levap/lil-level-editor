using System;
using System.Collections.Generic;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Layouts;

/// <summary>
/// Maps <see cref="EnGraphLayoutBuilder"/> values to layout builders and lists them for pickers.
/// </summary>
public static class GraphLayoutBuilderPicker
{
  /// <summary>Shared stress builder (builders are stateless).</summary>
  private static readonly StressLayoutBuilder StressBuilder = new();

  /// <summary>Shared ring builder (builders are stateless).</summary>
  private static readonly RingLayoutBuilder RingBuilder = new();

  /// <summary>Shared layered builder (builders are stateless).</summary>
  private static readonly LayeredLayoutBuilder LayeredBuilder = new();

  /// <summary>All known layout builders, in declaration order (the default first).</summary>
  public static IReadOnlyList<EnGraphLayoutBuilder> All { get; } = Enum.GetValues<EnGraphLayoutBuilder>();

  /// <summary>
  /// Gets the builder of a layout.
  /// </summary>
  /// <param name="builder">Layout.</param>
  /// <returns>Shared builder instance.</returns>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="builder"/> is not a known layout.</exception>
  public static GraphLayoutBuilder For(EnGraphLayoutBuilder builder) => builder switch
  {
    EnGraphLayoutBuilder.Stress => StressBuilder,
    EnGraphLayoutBuilder.Layered => LayeredBuilder,
    EnGraphLayoutBuilder.Ring => RingBuilder,
    _ => throw new ArgumentOutOfRangeException(nameof(builder), builder, "Unknown graph layout builder."),
  };

  /// <summary>
  /// Gets the name of a layout as shown to the user.
  /// </summary>
  /// <param name="builder">Layout.</param>
  /// <returns>Display name.</returns>
  public static string DisplayName(EnGraphLayoutBuilder builder) => builder.ToString();
}

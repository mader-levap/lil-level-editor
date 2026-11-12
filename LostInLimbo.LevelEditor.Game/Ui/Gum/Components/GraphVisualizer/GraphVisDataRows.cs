using System;
using System.Collections.Generic;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer;

/// <summary>
/// Measured widths of one key/value pair of the data panel.
/// </summary>
/// <param name="KeyWidth">Width of the key text, in pixels.</param>
/// <param name="ValueWidth">Width of the value text on one line, in pixels.</param>
public readonly record struct GraphVisDataPairSize(float KeyWidth, float ValueWidth);

/// <summary>
/// How the data panel lays out its key/value pairs.
/// </summary>
/// <param name="KeyColumnWidth">Width of the key column of single-row pairs, in pixels (0 when no pair is single-row).</param>
/// <param name="IsSingleRow">For each pair, in order: true if key and value share one row, false if the value goes below the key.</param>
public sealed record GraphVisDataRowLayout(float KeyColumnWidth, IReadOnlyList<bool> IsSingleRow);

/// <summary>
/// Decides which key/value pairs of the data panel fit on one row.
/// </summary>
/// <remarks>
/// Single-row pairs form two aligned columns: keys on the left, values starting at the same X.
/// A pair is single-row when its key fits the key column and its value fits between the value
/// column and the right edge. The key column width is chosen among the key widths so that as
/// many pairs as possible are single-row; on a tie the narrower column wins. Deterministic.
/// </remarks>
public static class GraphVisDataRows
{
  /// <summary>
  /// Lays out key/value pairs.
  /// </summary>
  /// <param name="pairs">Measured pairs, in display order.</param>
  /// <param name="availableWidth">Width available for a row, in pixels.</param>
  /// <param name="columnGap">Free space between the key column and the value, in pixels.</param>
  /// <returns>Layout.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="pairs"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="columnGap"/> is negative.</exception>
  public static GraphVisDataRowLayout Arrange(IReadOnlyList<GraphVisDataPairSize> pairs, float availableWidth, float columnGap)
  {
    ArgumentNullException.ThrowIfNull(pairs);
    if (!(columnGap >= 0)) throw new ArgumentOutOfRangeException(nameof(columnGap), columnGap, "Gap must not be negative.");

    // Try every key width as the column width; keep the one with the most single-row pairs.
    float bestColumn = 0;
    int bestCount = 0;
    foreach (GraphVisDataPairSize candidate in pairs)
    {
      float column = candidate.KeyWidth;
      int count = 0;
      foreach (GraphVisDataPairSize pair in pairs)
      {
        if (Fits(pair, column, availableWidth, columnGap)) count++;
      }

      if (count > bestCount || (count == bestCount && count > 0 && column < bestColumn))
      {
        bestColumn = column;
        bestCount = count;
      }
    }

    var isSingleRow = new bool[pairs.Count];
    if (bestCount == 0) return new GraphVisDataRowLayout(0, isSingleRow);

    for (int i = 0; i < pairs.Count; i++) isSingleRow[i] = Fits(pairs[i], bestColumn, availableWidth, columnGap);
    return new GraphVisDataRowLayout(bestColumn, isSingleRow);
  }

  /// <summary>
  /// Tells whether a pair fits on one row with a given key column.
  /// </summary>
  /// <param name="pair">Pair.</param>
  /// <param name="column">Key column width.</param>
  /// <param name="availableWidth">Row width.</param>
  /// <param name="columnGap">Gap between key column and value.</param>
  /// <returns>True if it fits.</returns>
  private static bool Fits(GraphVisDataPairSize pair, float column, float availableWidth, float columnGap)
  {
    return pair.KeyWidth <= column && column + columnGap + pair.ValueWidth <= availableWidth;
  }
}

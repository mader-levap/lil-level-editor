using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LostInLimbo.LevelEditor.Ui.Gum.Components.GraphVisualizer.Data;

/// <summary>
/// Immutable key/value data shown in the data panel, in a guaranteed order: the order the pairs
/// were given in. Keys are unique and can also be looked up directly.
/// </summary>
/// <remarks>
/// A plain <see cref="Dictionary{TKey, TValue}"/> happens to enumerate in insertion order today,
/// but does not promise it; the data panel's row order must not depend on that.
/// </remarks>
public sealed class GraphVisProperties : IReadOnlyDictionary<string, string>, IReadOnlyList<KeyValuePair<string, string>>
{
  /// <summary>Pairs in display order, with key lookup.</summary>
  private readonly OrderedDictionary<string, string> _pairs;

  /// <summary>
  /// Creates the properties from pairs in display order (copied).
  /// </summary>
  /// <param name="pairs">Key/value pairs, in display order.</param>
  /// <exception cref="ArgumentNullException"><paramref name="pairs"/> or a key or value is null.</exception>
  /// <exception cref="ArgumentException">A key occurs more than once.</exception>
  public GraphVisProperties(IEnumerable<KeyValuePair<string, string>> pairs)
  {
    ArgumentNullException.ThrowIfNull(pairs);

    _pairs = [];
    foreach ((string key, string value) in pairs)
    {
      ArgumentNullException.ThrowIfNull(key, nameof(pairs));
      ArgumentNullException.ThrowIfNull(value, nameof(pairs));
      if (!_pairs.TryAdd(key, value)) throw new ArgumentException($"Property key \"{key}\" occurs more than once.", nameof(pairs));
    }
  }

  /// <summary>No properties.</summary>
  public static GraphVisProperties Empty { get; } = new([]);

  /// <inheritdoc />
  public int Count => _pairs.Count;

  /// <summary>Keys, in display order.</summary>
  public IEnumerable<string> Keys => _pairs.Keys;

  /// <summary>Values, in display order.</summary>
  public IEnumerable<string> Values => _pairs.Values;

  /// <summary>Value of a key.</summary>
  /// <param name="key">Key.</param>
  /// <exception cref="KeyNotFoundException">There is no such key.</exception>
  public string this[string key] => _pairs[key];

  /// <summary>Pair at a position in display order.</summary>
  /// <param name="index">Position, 0 first.</param>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
  public KeyValuePair<string, string> this[int index] => _pairs.GetAt(index);

  /// <inheritdoc />
  public bool ContainsKey(string key) => _pairs.ContainsKey(key);

  /// <inheritdoc />
  public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value) => _pairs.TryGetValue(key, out value);

  /// <summary>Enumerates the pairs in display order.</summary>
  /// <returns>Enumerator.</returns>
  public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _pairs.GetEnumerator();

  /// <inheritdoc />
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

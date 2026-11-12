using LostInLimbo.LevelEditor.Ui.Gum.App.Editor;

namespace LostInLimbo.LevelEditor.Tests.Ui.Gum;

/// <summary>
/// Headless tests for <see cref="OverlaySwitcher"/> (toolbar overlay switching rules).
/// </summary>
public sealed class OverlaySwitcherTests
{
  /// <summary>Test double that records the last visibility it was given.</summary>
  private sealed class FakeView : IOverlayView
  {
    /// <summary>Last visibility set; null if never set.</summary>
    public bool? Visible { get; private set; }

    /// <summary>Number of <see cref="SetVisible"/> calls.</summary>
    public int Calls { get; private set; }

    /// <inheritdoc />
    public void SetVisible(bool visible)
    {
      Visible = visible;
      Calls++;
    }
  }

  /// <summary>
  /// Creates a switcher with both overlays registered.
  /// </summary>
  /// <param name="levelGen">Level generation view.</param>
  /// <param name="levelInfo">Level information view.</param>
  /// <returns>Switcher.</returns>
  private static OverlaySwitcher CreateSwitcher(out FakeView levelGen, out FakeView levelInfo)
  {
    levelGen = new FakeView();
    levelInfo = new FakeView();
    var switcher = new OverlaySwitcher();
    switcher.Register(EnOverlay.LevelGen, levelGen);
    switcher.Register(EnOverlay.LevelInfo, levelInfo);
    return switcher;
  }

  /// <summary>
  /// Verifies that registering a non-current view hides it.
  /// </summary>
  [Fact]
  public void Register_NonCurrentOverlay_HidesView()
  {
    // Arrange + Act: Register both views while nothing is open.
    OverlaySwitcher switcher = CreateSwitcher(out FakeView levelGen, out FakeView levelInfo);

    // Assert: Both start hidden; nothing is current.
    Assert.Equal(EnOverlay.None, switcher.Current);
    Assert.False(levelGen.Visible);
    Assert.False(levelInfo.Visible);
  }

  /// <summary>
  /// Verifies that toggling a closed overlay opens it and raises the change event.
  /// </summary>
  [Fact]
  public void Toggle_ClosedOverlay_ShowsItAndRaisesEvent()
  {
    // Arrange: Switcher with nothing open; capture events.
    OverlaySwitcher switcher = CreateSwitcher(out FakeView levelGen, out _);
    EnOverlay? raised = null;
    switcher.CurrentChanged += (_, overlay) => raised = overlay;

    // Act: Open level generation.
    switcher.Toggle(EnOverlay.LevelGen);

    // Assert: It is visible, current, and reported.
    Assert.True(levelGen.Visible);
    Assert.Equal(EnOverlay.LevelGen, switcher.Current);
    Assert.Equal(EnOverlay.LevelGen, raised);
  }

  /// <summary>
  /// Verifies that toggling another overlay hides the current one and shows the new one.
  /// </summary>
  [Fact]
  public void Toggle_OtherOverlay_SwitchesVisibleView()
  {
    // Arrange: Level generation is open.
    OverlaySwitcher switcher = CreateSwitcher(out FakeView levelGen, out FakeView levelInfo);
    switcher.Toggle(EnOverlay.LevelGen);

    // Act: Request level information.
    switcher.Toggle(EnOverlay.LevelInfo);

    // Assert: Only level information is visible.
    Assert.False(levelGen.Visible);
    Assert.True(levelInfo.Visible);
    Assert.Equal(EnOverlay.LevelInfo, switcher.Current);
  }

  /// <summary>
  /// Verifies that toggling the current overlay again closes it.
  /// </summary>
  [Fact]
  public void Toggle_CurrentOverlay_ClosesIt()
  {
    // Arrange: Level generation is open.
    OverlaySwitcher switcher = CreateSwitcher(out FakeView levelGen, out _);
    switcher.Toggle(EnOverlay.LevelGen);

    // Act: Click the same overlay again.
    switcher.Toggle(EnOverlay.LevelGen);

    // Assert: It is hidden and nothing is current.
    Assert.False(levelGen.Visible);
    Assert.Equal(EnOverlay.None, switcher.Current);
  }

  /// <summary>
  /// Verifies that requesting None closes the current overlay.
  /// </summary>
  [Fact]
  public void Toggle_None_ClosesCurrentOverlay()
  {
    // Arrange: Level information is open.
    OverlaySwitcher switcher = CreateSwitcher(out _, out FakeView levelInfo);
    switcher.Toggle(EnOverlay.LevelInfo);

    // Act: Request no overlay.
    switcher.Toggle(EnOverlay.None);

    // Assert: Closed.
    Assert.False(levelInfo.Visible);
    Assert.Equal(EnOverlay.None, switcher.Current);
  }

  /// <summary>
  /// Verifies that requesting an overlay without a registered view closes the current one.
  /// </summary>
  [Fact]
  public void Toggle_UnregisteredOverlay_ClosesCurrentOverlay()
  {
    // Arrange: Only level generation is registered and open.
    var levelGen = new FakeView();
    var switcher = new OverlaySwitcher();
    switcher.Register(EnOverlay.LevelGen, levelGen);
    switcher.Toggle(EnOverlay.LevelGen);

    // Act: Request the unregistered level information overlay.
    switcher.Toggle(EnOverlay.LevelInfo);

    // Assert: Nothing is open.
    Assert.False(levelGen.Visible);
    Assert.Equal(EnOverlay.None, switcher.Current);
  }

  /// <summary>
  /// Verifies that a request that changes nothing does not raise the change event.
  /// </summary>
  [Fact]
  public void Show_SameAsCurrent_DoesNotRaiseEvent()
  {
    // Arrange: Nothing open; count events.
    OverlaySwitcher switcher = CreateSwitcher(out _, out _);
    int events = 0;
    switcher.CurrentChanged += (_, _) => events++;

    // Act: Request "none" while nothing is open.
    switcher.Show(EnOverlay.None);

    // Assert: No event.
    Assert.Equal(0, events);
  }

  /// <summary>
  /// Verifies that replacing the view of the open overlay hides the old view and shows the new one
  /// (used when a UGS overlay is swapped for its Gum port).
  /// </summary>
  [Fact]
  public void Register_ReplacingCurrentView_HidesOldAndShowsNew()
  {
    // Arrange: Level generation is open with its first view.
    OverlaySwitcher switcher = CreateSwitcher(out FakeView oldView, out _);
    switcher.Toggle(EnOverlay.LevelGen);
    var newView = new FakeView();

    // Act: Replace the view.
    switcher.Register(EnOverlay.LevelGen, newView);

    // Assert: Old hidden, new visible, overlay still current.
    Assert.False(oldView.Visible);
    Assert.True(newView.Visible);
    Assert.Equal(EnOverlay.LevelGen, switcher.Current);
  }

  /// <summary>
  /// Verifies that None cannot be registered.
  /// </summary>
  [Fact]
  public void Register_None_Throws()
  {
    // Arrange: Empty switcher.
    var switcher = new OverlaySwitcher();

    // Act + Assert: Registering a view for None is rejected.
    Assert.Throws<ArgumentException>(() => switcher.Register(EnOverlay.None, new FakeView()));
  }
}

using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Access;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Numerics;

namespace Content.Client.Access.UI;

[GenerateTypedNameReferences]
public sealed partial class GroupedAccessLevelChecklist : BoxContainer
{
    private static readonly ProtoId<AccessGroupPrototype> GeneralAccessGroup = "General";

    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    private bool _isMonotone;
    private string? _labelStyleClass;

    // Access data
    private HashSet<ProtoId<AccessGroupPrototype>> _accessGroups = new();
    private HashSet<ProtoId<AccessLevelPrototype>> _accessLevels = new();
    private HashSet<ProtoId<AccessLevelPrototype>> _activeAccessLevels = new();

    // Button groups
    private readonly ButtonGroup _accessGroupsButtons = new();

    // Temp values
    private int _accessGroupTabIndex = 0;
    private bool _canInteract = false;
    private List<AccessLevelPrototype> _accessLevelsForTab = new();
    private readonly List<AccessLevelEntry> _accessLevelEntries = new();
    private readonly Dictionary<AccessGroupPrototype, List<AccessLevelPrototype>> _groupedAccessLevels = new();

    // Events
    public event Action<HashSet<ProtoId<AccessLevelPrototype>>, bool>? OnAccessLevelsChangedEvent;

    /// <summary>
    /// Creates a UI control for changing access levels.
    /// Access levels are organized under a list of tabs by their associated access group.
    /// </summary>
    public GroupedAccessLevelChecklist()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
    }

    private void ArrangeAccessControls()
    {
        // Create a list of known access groups with which to populate the UI
        _groupedAccessLevels.Clear();

        foreach (var accessGroup in _accessGroups)
        {
            if (!_protoManager.TryIndex(accessGroup, out var accessGroupProto))
                continue;

            _groupedAccessLevels.Add(accessGroupProto, new());
        }

        // Ensure that the 'general' access group is added to handle
        // misc. access levels that aren't associated with any group
        if (_protoManager.TryIndex(GeneralAccessGroup, out var generalAccessProto))
            _groupedAccessLevels.TryAdd(generalAccessProto, new());

        // Assign known access levels with their associated groups
        foreach (var accessLevel in _accessLevels)
        {
            if (!_protoManager.TryIndex(accessLevel, out var accessLevelProto))
                continue;

            var assigned = false;

            foreach (var (accessGroup, accessLevels) in _groupedAccessLevels)
            {
                if (!accessGroup.Tags.Contains(accessLevelProto.ID))
                    continue;

                assigned = true;
                _groupedAccessLevels[accessGroup].Add(accessLevelProto);
            }

            if (!assigned && generalAccessProto != null)
                _groupedAccessLevels[generalAccessProto].Add(accessLevelProto);
        }

        // Remove access groups that have no assigned access levels
        foreach (var (group, accessLevels) in _groupedAccessLevels)
        {
            if (accessLevels.Count == 0)
                _groupedAccessLevels.Remove(group);
        }
    }

    private bool TryRebuildAccessGroupControls()
    {
        AccessGroupList.DisposeAllChildren();
        AccessLevelChecklist.DisposeAllChildren();

        // No access level prototypes were assigned to any of the access level groups.
        // Either the turret controller has no assigned access levels or their names were invalid.
        if (_groupedAccessLevels.Count == 0)
            return false;

        // Reorder the access groups alphabetically
        var orderedAccessGroups = _groupedAccessLevels.Keys.OrderBy(x => x.GetAccessGroupName()).ToList();

        // Add group access buttons to the UI
        foreach (var accessGroup in orderedAccessGroups)
        {
            var accessGroupButton = CreateAccessGroupButton();

            // Button styling
            if (_groupedAccessLevels.Count > 1)
            {
                if (AccessGroupList.ChildCount == 0)
                    accessGroupButton.AddStyleClass(StyleBase.ButtonOpenLeft);
                else if (_groupedAccessLevels.Count > 1 && AccessGroupList.ChildCount == (_groupedAccessLevels.Count - 1))
                    accessGroupButton.AddStyleClass(StyleBase.ButtonOpenRight);
                else
                    accessGroupButton.AddStyleClass(StyleBase.ButtonOpenBoth);
            }

            accessGroupButton.Pressed = _accessGroupTabIndex == orderedAccessGroups.IndexOf(accessGroup);

            // Label text and styling
            if (_labelStyleClass != null)
                accessGroupButton.Label.SetOnlyStyleClass(_labelStyleClass);

            var accessLevelPrototypes = _groupedAccessLevels[accessGroup];
            var prefix = accessLevelPrototypes.All(x => _activeAccessLevels.Contains(x))
                ? "»"
                : accessLevelPrototypes.Any(x => _activeAccessLevels.Contains(x))
                    ? "›"
                    : " ";

            var text = Loc.GetString(
                "turret-controls-window-access-group-label",
                ("prefix", prefix),
                ("label", accessGroup.GetAccessGroupName())
            );

            accessGroupButton.Text = text;

            // Button events
            accessGroupButton.OnPressed += _ => OnAccessGroupChanged(accessGroupButton.GetPositionInParent());

            AccessGroupList.AddChild(accessGroupButton);
        }

        // Adjust the current tab index so it remains in range
        if (_accessGroupTabIndex >= _groupedAccessLevels.Count)
            _accessGroupTabIndex = _groupedAccessLevels.Count - 1;

        return true;
    }

    /// <summary>
    /// Rebuilds the checkbox list for the access level controls.
    /// </summary>
    public void RebuildAccessLevelsControls()
    {
        AccessLevelChecklist.DisposeAllChildren();
        _accessLevelEntries.Clear();

        // No access level prototypes were assigned to any of the access level groups
        // Either turret controller has no assigned access levels, or their names were invalid
        if (_groupedAccessLevels.Count == 0)
            return;

        // Reorder the access groups alphabetically
        var orderedAccessGroups = _groupedAccessLevels.Keys.OrderBy(x => x.GetAccessGroupName()).ToList();

        // Get the access levels associated with the current tab
        var selectedAccessGroupTabProto = orderedAccessGroups[_accessGroupTabIndex];
        _accessLevelsForTab = _groupedAccessLevels[selectedAccessGroupTabProto];
        _accessLevelsForTab = _accessLevelsForTab.OrderBy(x => x.GetAccessLevelName()).ToList();

        // Add an 'all' checkbox as the first child of the list if it has more than one access level
        // Toggling this checkbox on will mark all other boxes below it on/off
        var allCheckBox = CreateAccessLevelCheckbox();
        allCheckBox.Text = Loc.GetString("turret-controls-window-all-checkbox");

        if (_labelStyleClass != null)
            allCheckBox.Label.SetOnlyStyleClass(_labelStyleClass);

        // Add the 'all' checkbox events
        allCheckBox.OnPressed += args =>
        {
            SetCheckBoxPressedState(_accessLevelEntries, allCheckBox.Pressed);

            var accessLevels = new HashSet<ProtoId<AccessLevelPrototype>>();

            foreach (var accessLevel in _accessLevelsForTab)
            {
                accessLevels.Add(accessLevel);
            }

            OnAccessLevelsChangedEvent?.Invoke(accessLevels, allCheckBox.Pressed);
        };

        AccessLevelChecklist.AddChild(allCheckBox);

        // Hide the 'all' checkbox if the tab has only one access level
        var allCheckBoxVisible = _accessLevelsForTab.Count > 1;

        allCheckBox.Visible = allCheckBoxVisible;
        allCheckBox.Disabled = !_canInteract;

        // Add any remaining missing access level buttons to the UI
        foreach (var accessLevel in _accessLevelsForTab)
        {
            // Create the entry
            var accessLevelEntry = new AccessLevelEntry(_isMonotone);

            accessLevelEntry.AccessLevel = accessLevel;
            accessLevelEntry.CheckBox.Text = accessLevel.GetAccessLevelName();
            accessLevelEntry.CheckBox.Pressed = _activeAccessLevels.Contains(accessLevel);
            accessLevelEntry.CheckBox.Disabled = !_canInteract;

            if (_labelStyleClass != null)
                accessLevelEntry.CheckBox.Label.SetOnlyStyleClass(_labelStyleClass);

            // Set the checkbox linkage lines
            var isEndOfList = _accessLevelsForTab.IndexOf(accessLevel) == (_accessLevelsForTab.Count - 1);

            var lines = new List<(Vector2, Vector2)>
            {
                (new Vector2(0.5f, 0f), new Vector2(0.5f, isEndOfList ? 0.5f : 1f)),
                (new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f)),
            };

            accessLevelEntry.UpdateCheckBoxLink(lines);
            accessLevelEntry.CheckBoxLink.Visible = allCheckBoxVisible;
            accessLevelEntry.CheckBoxLink.Modulate = !_canInteract ? Color.Gray : Color.White;

            // Add checkbox events
            accessLevelEntry.CheckBox.OnPressed += args =>
            {
                // If the checkbox and its siblings are checked, check the 'all' checkbox too
                allCheckBox.Pressed = AreAllCheckBoxesPressed(_accessLevelEntries.Select(x => x.CheckBox));

                OnAccessLevelsChangedEvent?.Invoke([accessLevelEntry.AccessLevel], accessLevelEntry.CheckBox.Pressed);
            };

            AccessLevelChecklist.AddChild(accessLevelEntry);
            _accessLevelEntries.Add(accessLevelEntry);
        }

        // Press the 'all' checkbox if all others are pressed
        allCheckBox.Pressed = AreAllCheckBoxesPressed(_accessLevelEntries.Select(x => x.CheckBox));
    }

    private bool AreAllCheckBoxesPressed(IEnumerable<CheckBox> checkBoxes)
    {
        foreach (var checkBox in checkBoxes)
        {
            if (!checkBox.Pressed)
                return false;
        }

        return true;
    }

    private void SetCheckBoxPressedState(List<AccessLevelEntry> accessLevelEntries, bool pressed)
    {
        foreach (var accessLevelEntry in accessLevelEntries)
        {
            accessLevelEntry.CheckBox.Pressed = pressed;
        }
    }


    /// <summary>
    /// Provides the UI with a list of access groups using which list of tabs should be populated.
    /// </summary>
    public void SetAccessGroups(HashSet<ProtoId<AccessGroupPrototype>> accessGroups)
    {
        _accessGroups = accessGroups;

        ArrangeAccessControls();

        if (TryRebuildAccessGroupControls())
            RebuildAccessLevelsControls();
    }

    /// <summary>
    /// Provides the UI with a list of access levels with which it can populate the currently selected tab.
    /// </summary>
    public void SetAccessLevels(HashSet<ProtoId<AccessLevelPrototype>> accessLevels)
    {
        _accessLevels = accessLevels;

        ArrangeAccessControls();

        if (TryRebuildAccessGroupControls())
            RebuildAccessLevelsControls();
    }

    /// <summary>
    /// Sets which access level checkboxes should be marked on the UI.
    /// </summary>
    public void SetActiveAccessLevels(HashSet<ProtoId<AccessLevelPrototype>> activeAccessLevels)
    {
        _activeAccessLevels = activeAccessLevels;

        if (TryRebuildAccessGroupControls())
            RebuildAccessLevelsControls();
    }

    /// <summary>
    /// Sets whether the local player can interact with the checkboxes.
    /// </summary>
    public void SetLocalPlayerAccessibility(bool canInteract)
    {
        _canInteract = canInteract;

        if (TryRebuildAccessGroupControls())
            RebuildAccessLevelsControls();
    }

    /// <summary>
    /// Sets whether the UI should use monotone buttons and checkboxes.
    /// </summary>
    public void SetMonotone(bool monotone)
    {
        _isMonotone = monotone;

        if (TryRebuildAccessGroupControls())
            RebuildAccessLevelsControls();
    }

    /// <summary>
    /// Applies the specified style to the labels on the UI buttons and checkboxes.
    /// </summary>
    public void SetLabelStyleClass(string? styleClass)
    {
        _labelStyleClass = styleClass;

        if (TryRebuildAccessGroupControls())
            RebuildAccessLevelsControls();
    }

    private void OnAccessGroupChanged(int newTabIndex)
    {
        if (newTabIndex == _accessGroupTabIndex)
            return;

        _accessGroupTabIndex = newTabIndex;

        if (TryRebuildAccessGroupControls())
            RebuildAccessLevelsControls();
    }

    private Button CreateAccessGroupButton()
    {
        var button = _isMonotone ? new MonotoneButton() : new Button();

        button.ToggleMode = true;
        button.Group = _accessGroupsButtons;
        button.Label.HorizontalAlignment = HAlignment.Left;

        return button;
    }

    private CheckBox CreateAccessLevelCheckbox()
    {
        var checkbox = _isMonotone ? new MonotoneCheckBox() : new CheckBox();

        checkbox.Margin = new Thickness(0, 0, 0, 3);
        checkbox.ToggleMode = true;
        checkbox.ReservesSpace = false;

        return checkbox;
    }

    private sealed class AccessLevelEntry : BoxContainer
    {
        public ProtoId<AccessLevelPrototype> AccessLevel;
        public readonly CheckBox CheckBox;
        public readonly LineRenderer CheckBoxLink;

        public AccessLevelEntry(bool monotone)
        {
            HorizontalExpand = true;

            CheckBoxLink = new LineRenderer
            {
                SetWidth = 22,
                VerticalExpand = true,
                Margin = new Thickness(0, -1),
                ReservesSpace = false,
            };

            AddChild(CheckBoxLink);

            CheckBox = monotone ? new MonotoneCheckBox() : new CheckBox();
            CheckBox.ToggleMode = true;
            CheckBox.Margin = new Thickness(0f, 0f, 0f, 3f);

            AddChild(CheckBox);
        }

        public void UpdateCheckBoxLink(List<(Vector2, Vector2)> lines)
        {
            CheckBoxLink.Lines = lines;
        }
    }

    private sealed class LineRenderer : Control
    {
        /// <summary>
        /// List of lines to render (their start and end x-y coordinates).
        /// Position (0,0) is the top left corner of the control and
        /// position (1,1) is the bottom right corner.
        /// </summary>
        /// <remarks>
        /// The color of the lines is inherited from the control.
        /// </remarks>
        public List<(Vector2, Vector2)> Lines;

        public LineRenderer()
        {
            Lines = new List<(Vector2, Vector2)>();
        }

        public LineRenderer(List<(Vector2, Vector2)> lines)
        {
            Lines = lines;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            foreach (var line in Lines)
            {
                var start = PixelPosition +
                            new Vector2(PixelWidth * line.Item1.X, PixelHeight * line.Item1.Y);

                var end = PixelPosition +
                          new Vector2(PixelWidth * line.Item2.X, PixelHeight * line.Item2.Y);

                handle.DrawLine(start, end, ActualModulateSelf);
            }
        }
    }
}

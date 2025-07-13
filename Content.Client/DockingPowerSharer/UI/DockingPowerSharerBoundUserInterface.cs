using Content.Shared.DockingPowerSharer;
using Content.Shared.Power;
using Robust.Client.UserInterface;

namespace Content.Client.DockingPowerSharer.UI;

public sealed class DockingPowerSharerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private DockingPowerSharerWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<DockingPowerSharerWindow>();

        _window.VoltageChanged += OnVoltageChanged;

        Update();
    }

    private void OnVoltageChanged(Voltage newVoltage)
    {
        SendPredictedMessage(new DockingPowerSharerVoltageChangeMessage(newVoltage));
    }
}

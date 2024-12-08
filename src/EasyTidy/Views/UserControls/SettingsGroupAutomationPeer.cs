using Microsoft.UI.Xaml.Automation.Peers;

namespace EasyTidy.Views.UserControls;

public partial class SettingsGroupAutomationPeer : FrameworkElementAutomationPeer
{
    public SettingsGroupAutomationPeer(SettingsGroup owner)
            : base(owner)
    {
    }

    protected override string GetNameCore()
    {
        var selectedSettingsGroup = (SettingsGroup)Owner;
        return selectedSettingsGroup.Header;
    }
}

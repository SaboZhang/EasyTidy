using Microsoft.UI.Xaml.Automation.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

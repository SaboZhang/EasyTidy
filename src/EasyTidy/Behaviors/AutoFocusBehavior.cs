using CommunityToolkit.WinUI.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Behaviors;

public class AutoFocusBehavior : BehaviorBase<Control>
{
    protected override void OnAssociatedObjectLoaded() => AssociatedObject.Focus(FocusState.Programmatic);
}

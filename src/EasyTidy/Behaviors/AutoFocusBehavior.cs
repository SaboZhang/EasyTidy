using CommunityToolkit.WinUI.Behaviors;

namespace EasyTidy.Behaviors;

public class AutoFocusBehavior : BehaviorBase<Control>
{
    protected override void OnAssociatedObjectLoaded() => AssociatedObject.Focus(FocusState.Programmatic);
}

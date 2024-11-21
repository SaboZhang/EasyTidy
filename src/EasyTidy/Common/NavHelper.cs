using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Common;

public class NavHelper
{
    public static Type GetNavigateTo(NavigationViewItem item)
    {
        return (Type)item?.GetValue(NavigateToProperty);
    }

    public static void SetNavigateTo(NavigationViewItem item, Type value)
    {
        item?.SetValue(NavigateToProperty, value);
    }

    public static readonly DependencyProperty NavigateToProperty =
           DependencyProperty.RegisterAttached("NavigateTo", typeof(Type), typeof(NavHelper), new PropertyMetadata(null));
}

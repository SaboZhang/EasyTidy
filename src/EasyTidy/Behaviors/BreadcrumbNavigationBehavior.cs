using EasyTidy.Common.Model;
using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Behaviors;

public class BreadcrumbNavigationBehavior : Behavior<BreadcrumbBar>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.ItemClicked += OnClick;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.ItemClicked -= OnClick;
    }

    private void OnClick(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        var crumb = args.Item as Breadcrumb;
        crumb?.NavigateTo();

        if (crumb == null)
        {
            Logger.Info("BreadcrumbBarItemClickedEventArgs.Item is not a Breadcrumb");
        }
    }
}

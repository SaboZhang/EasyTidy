using EasyTidy.Contracts.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Common.Model;

public class Breadcrumb
{
    public Breadcrumb(string label, string page)
    {
        Label = label;
        Page = page;
    }

    public string Label { get; }

    public string Page { get; }

    public override string ToString() => Label;

    public void NavigateTo()
    {
        var navigationService = App.GetService<INavigationService>();
        navigationService.NavigateTo(Page);
    }
}

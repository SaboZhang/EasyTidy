using EasyTidy.Contracts.Service;
using System.Collections.ObjectModel;

namespace EasyTidy.ViewModels;
public partial class BreadCrumbBarViewModel : ObservableRecipient
{
    [ObservableProperty]
    public ObservableCollection<string> breadcrumbBarCollection;

    private INavigationService _navigationService;

    public BreadCrumbBarViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        breadcrumbBarCollection = [];
    }

    [RelayCommand]
    private void OnItemClick(BreadcrumbBarItemClickedEventArgs args)
    {
        int numItemsToGoBack = BreadcrumbBarCollection.Count - args.Index - 1;
        for (int i = 0; i < numItemsToGoBack; i++)
        {
            _navigationService.GoBack();
        }
    }
}

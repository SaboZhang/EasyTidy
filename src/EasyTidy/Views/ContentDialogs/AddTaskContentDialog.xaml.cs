// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using CommunityToolkit.WinUI;
using EasyTidy.Model;
using Microsoft.UI.Xaml.Controls.Primitives;
using Quartz.Util;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace EasyTidy.Views.ContentDialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AddTaskContentDialog : ContentDialog, INotifyDataErrorInfo, INotifyPropertyChanged
{
    public TaskOrchestrationViewModel ViewModel { get; set; }

    private string _groupName;
    private string _taskRule;
    private bool _isRegex = false;
    private TaskRuleType _ruleType = TaskRuleType.CustomRule;

    private bool _isShowTaskSource = true;

    public string GroupName
    {
        get => _groupName;
        set
        {
            if (_groupName != value)
            {
                _groupName = value;
                ValidateGroupName(_groupName);
                OnPropertyChanged();
            }
        }
    }

    public string TaskRule
    {
        get => _taskRule;
        set
        {
            if (_taskRule != value)
            {
                _taskRule = value;
                OnPropertyChanged();
            }
        }
    }

    public TaskRuleType RuleType
    {
        get => _ruleType;
        set
        {
            if (_ruleType != value)
            {
                _ruleType = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsShowTaskSource
    {
        get => _isShowTaskSource;
        set
        {
            if (_isShowTaskSource != value)
            {
                _isShowTaskSource = value;
                OnPropertyChanged();
            }
        }
    }

    public string TaskName { get; set; }

    public string TaskSource { get; set; }

    public bool Shortcut { get; set; }

    public string TaskTarget { get; set; }

    public bool EnabledFlag { get; set; } = true;

    public int Priority { get; set; } = 0;

    public bool IsRegex
    {
        get => _isRegex;
        set
        {
            if (_isRegex != value)
            {
                _isRegex = value;
                OnPropertyChanged();
            }
        }
    }

    public AddTaskContentDialog()
    {
        ViewModel = App.GetService<TaskOrchestrationViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
        RequestedTheme = ViewModel.ThemeSelectorService.Theme;
        PopulateMenu(ViewModel);
        ValidTextBlock.Visibility = Visibility.Collapsed;
    }

    private void PopulateMenu(TaskOrchestrationViewModel viewModel)
    {
        foreach (var category in viewModel.MenuCategories)
        {
            // 创建第一级菜单项
            var subItem = new MenuFlyoutSubItem { Text = category.Title };

            // 添加第二级菜单项
            for (int i = 0; i < category.Items.Count; i++)
            {
                var menuItem = new MenuFlyoutItem { Text = category.Items[i] };
                menuItem.Click += OnMenuItemClick;
                subItem.Items.Add(menuItem);
                if (i == 1 || i == 7)
                {
                    subItem.Items.Add(new MenuFlyoutSeparator());
                }
            }

            // 将 SubItem 添加到主 MenuFlyout
            RuleFlyout.Items.Add(subItem);
        }
        // 在所有主菜单项之后添加 ToggleMenuFlyoutItem
        var toggleItem = new ToggleMenuFlyoutItem
        {
            Text = "RegardedExpressionText".GetLocalized(),
            IsChecked = IsRegex
        };

        // 注册 Click 事件处理程序
        toggleItem.Click += (sender, e) =>
        {
            // 切换 IsChecked 状态
            IsRegex = !IsRegex;
            // 调用方法以处理状态变化
            OnIsRegexChanged(IsRegex);
        };

        // 添加可选的分隔符和 ToggleMenuFlyoutItem 到主 MenuFlyout
        RuleFlyout.Items.Add(new MenuFlyoutSeparator());  // 分隔符
        RuleFlyout.Items.Add(toggleItem);                 // ToggleMenuFlyoutItem
    }


    private void OnIsRegexChanged(bool isRegex)
    {
        IsRegex = isRegex;
        if (IsRegex)
        {
            RuleType = TaskRuleType.ExpressionRules;
        }
    }

    private void OnMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem)
        {
            var selectedValue = menuItem.Text;
            var rule = RuleRegex().Split(selectedValue)[0];
            TaskRule = rule;

            foreach (var item in RuleFlyout.Items)
            {
                if (item is MenuFlyoutSubItem subItem)
                {
                    if (subItem.Text == "HandlingFolderRules".GetLocalized() && subItem.Items.Contains(menuItem))
                    {
                        RuleType = TaskRuleType.FolderRule;
                        break;
                    }
                    else if (subItem.Text == "HandlingRulesForFiles".GetLocalized() && subItem.Items.Contains(menuItem))
                    {
                        RuleType = TaskRuleType.FileRule;
                        break;
                    }
                    else
                    {
                        RuleType = TaskRuleType.CustomRule;
                    }
                }
            }
        }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex RuleRegex();

    private void FilterButtonTeachingTip_CloseButtonClick(TeachingTip sender, object args)
    {
        ViewModel.SelectedItemChangedCommand.Execute(sender);
    }

    private void ValidateGroupName(string groupName)
    {
        var errors = new List<string>(2);
        if (string.IsNullOrWhiteSpace(groupName))
        {
            errors.Add("GroupInformationVerification".GetLocalized());
            errors.Add("GroupInformationVerificationAdd".GetLocalized());
        }
        SetErrors("GroupName", errors);
    }


    public bool HasErrors => _validationErrors.Count > 0;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new(propertyName));

    // Error validation
    private readonly Dictionary<string, ICollection<string>> _validationErrors = [];

    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
    public event PropertyChangedEventHandler PropertyChanged;

    private void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new(propertyName));
        OnPropertyChanged(nameof(HasErrors));
    }

    public IEnumerable GetErrors(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) ||
            !_validationErrors.ContainsKey(propertyName))
            return null;

        return _validationErrors[propertyName];
    }

    private void SetErrors(string key, ICollection<string> errors)
    {
        if (errors.Count != 0)
            _validationErrors[key] = errors;
        else
            _ = _validationErrors.Remove(key);

        OnErrorsChanged(key);
    }

    private void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        FilterButtonTeachingTip.IsOpen = true;
    }

    private void TaskOperateList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender != null)
        {
            var selected = sender as ComboBox;
            if (selected != null && selected.SelectedItem != null)
            {
                switch (selected.SelectedItem)
                {
                    case OperationMode.Delete:
                        RenameButton.Visibility = Visibility.Collapsed;
                        break;
                    case OperationMode.RecycleBin:
                        TaskSourcePanel.Visibility = Visibility.Collapsed;
                        RenameButton.Visibility = Visibility.Collapsed;
                        break;
                    case OperationMode.Rename:
                        RenameButton.Visibility = Visibility.Visible;
                        TaskTargetTitle.Text = "NewNameAndPath".GetLocalized();
                        TaskSourcePanel.Visibility = Visibility.Collapsed;
                        break;
                    default:
                        RenameButton.Visibility = Visibility.Collapsed;
                        TaskSourcePanel.Visibility = Visibility.Visible;
                        break;
                }
            }
        }
    }

    private void RenameItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is PatternSnippetModel s)
        {
            RenameFlyout.Hide();
            Target.Text += "\\" + s.Code;
        }
    }

    private void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        var priority = sender  as ToggleButton;
        if (priority.IsChecked == true)
        {
            Priority += 5;
        }
        else
        {
            Priority = 0;
        }
    }

    private void TaskGroupNameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var res = ValidTextBlock.Tag;
        if (res.ToString() == "False" && string.IsNullOrWhiteSpace(TaskGroupNameBox.Text))
        {
            ValidTextBlock.Visibility = Visibility.Visible;
            ValidTaskGroupNameBox.Text = "GroupInformationVerificationAdd".GetLocalized() + "\n" + "GroupInformationVerification".GetLocalized();
        }
        else if (string.IsNullOrWhiteSpace(TaskGroupNameBox.Text))
        {
            ValidTextBlock.Visibility = Visibility.Visible;
            ValidTaskGroupNameBox.Text = "GroupInformationVerificationAdd".GetLocalized() + "\n" + "GroupInformationVerification".GetLocalized();
        } 
        else if (res.ToString() == "True" && !string.IsNullOrWhiteSpace(TaskGroupNameBox.Text))
        {
            ValidTextBlock.Visibility = Visibility.Collapsed;
        }
        else if (res.ToString() == "False" &&!string.IsNullOrWhiteSpace(TaskGroupNameBox.Text))
        {
            ValidTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}

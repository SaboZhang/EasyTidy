// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using CommunityToolkit.WinUI;
using EasyTidy.Model;
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

    public bool IsValid { get; set; }

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
            var subItem = new MenuFlyoutSubItem { Text = category.Title };

            for (int i = 0; i < category.Items.Count; i++)
            {
                var menuItem = new MenuFlyoutItem { Text = category.Items[i] };
                menuItem.Click += OnMenuItemClick;
                subItem.Items.Add(menuItem);
                if (i == 1 || i == 6)
                {
                    subItem.Items.Add(new MenuFlyoutSeparator());
                }
            }

            RuleFlyout.Items.Add(subItem);
        }

        var toggleItem = new ToggleMenuFlyoutItem
        {
            Text = "RegardedExpressionText".GetLocalized(),
            IsChecked = IsRegex
        };

        toggleItem.Click += (sender, e) =>
        {
            IsRegex = !IsRegex;
            OnIsRegexChanged(IsRegex);
            toggleItem.IsChecked = IsRegex;
        };
        DispatcherQueue.TryEnqueue(() =>
        {
            toggleItem.IsChecked = IsRegex;
        });

        RuleFlyout.Items.Add(new MenuFlyoutSeparator()); 
        RuleFlyout.Items.Add(toggleItem);                 // ToggleMenuFlyoutItem
    }


    private void OnIsRegexChanged(bool isRegex)
    {
        IsRegex = isRegex;
        if (IsRegex)
        {
            RuleType = TaskRuleType.ExpressionRules;
        }
        else
        {
            RuleType = RuleType == TaskRuleType.ExpressionRules ? TaskRuleType.CustomRule : RuleType;
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
                        TaskTargetPanel.Visibility = Visibility.Visible;
                        TaskSourcePanel.Visibility = Visibility.Collapsed;
                        break;
                    case OperationMode.RecycleBin:
                        TaskSourcePanel.Visibility = Visibility.Collapsed;
                        RenameButton.Visibility = Visibility.Collapsed;
                        TaskTargetPanel.Visibility = Visibility.Visible;
                        break;
                    case OperationMode.Rename:
                        RenameButton.Visibility = Visibility.Visible;
                        TaskTargetTitle.Text = "NewNameAndPath".GetLocalized();
                        TaskSourcePanel.Visibility = Visibility.Collapsed;
                        TaskTargetPanel.Visibility = Visibility.Visible;
                        break;
                    case OperationMode.UploadWebDAV:
                        TaskTargetPanel.Visibility = Visibility.Collapsed;
                        RenameButton.Visibility = Visibility.Collapsed;
                        TaskSourcePanel.Visibility = Visibility.Visible;
                        ViewModel.TaskTarget = ViewModel.TaskSource;
                        break;
                    default:
                        RenameButton.Visibility = Visibility.Collapsed;
                        TaskSourcePanel.Visibility = Visibility.Visible;
                        TaskTargetPanel.Visibility = Visibility.Visible;
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

    private void TaskGroupNameBox_LosingFocus(UIElement sender, Microsoft.UI.Xaml.Input.LosingFocusEventArgs args)
    {
        if (ViewModel.DialogClosed) 
        {
            ViewModel.DialogClosed = false;
            return;
        }
        var text = (sender as TextBox)?.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            ValidTextBlock.Visibility = Visibility.Visible;
            ValidTaskGroupNameBox.Text = "GroupInformationVerificationAdd".GetLocalized() + "\n" + "GroupInformationVerification".GetLocalized();
            IsValid = false;
        }
        else
        {
            ValidTextBlock.Visibility = Visibility.Collapsed;
            IsValid = true;
        }
    }

    private void ValidateRuleString_LosingFocus(UIElement sender, Microsoft.UI.Xaml.Input.LosingFocusEventArgs args)
    {
        var text = (sender as TextBox)?.Text;
        IsValid = ValidateRuleString(text);
        if (!IsValid && !string.IsNullOrWhiteSpace(text)) 
        {
            // 验证未通过显示错误信息
            TaskRuleBoxValid.Visibility = Visibility.Visible;
        }
        else
        {
            // 隐藏错误提示框
            TaskRuleBoxValid.Visibility = Visibility.Collapsed;
        }
    }

    private static bool ValidateRuleString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // 检查是否使用分号或竖线分割
        bool containsSeparator = input.Contains(';') || input.Contains('|');

        // 检查是否包含 *
        bool containsAsterisk = input.Contains('*');

        // 检查是否包含 #
        bool containsHash = input.Contains('#');

        // 条件1: 包含分隔符且包含 *
        if (containsSeparator && containsAsterisk)
        {
            return true;
        }

        // 条件2: 不包含分隔符，但包含 * 或 #
        if (!containsSeparator && (containsAsterisk || containsHash))
        {
            return true;
        }

        // 条件3: 合法的正则表达式
        return IsValidRegex(input);
    }

    private static bool IsValidRegex(string pattern)
    {
        try
        {
            _ = Regex.IsMatch("", pattern); // 尝试匹配空字符串
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

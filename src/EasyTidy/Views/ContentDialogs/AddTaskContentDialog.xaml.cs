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
    private string _customPrompt;
    private string _systemPrompt;
    private string _userPrompt;

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

    public string Password { get; set; }

    public Encrypted Encencrypted { get; set; }

    public bool IsSourceFile { get; set; }

    public string CustomPrompt
    {
        get => _customPrompt;
        set
        {
            if (_customPrompt != value)
            {
                _customPrompt = value;
                OnPropertyChanged();
            }
        }
    }

    public string SystemPrompt
    {
        get => _systemPrompt;
        set
        {
            if (_systemPrompt != value)
            {
                _systemPrompt = value;
                OnPropertyChanged();
            }
        }
    }

    public string UserPrompt
    {
        get => _userPrompt;
        set
        {
            if (_userPrompt != value)
            {
                _userPrompt = value;
                OnPropertyChanged();
            }
        }
    }

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

    private PromptType _selectedMode = PromptType.BuiltIn;
    public PromptType SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (_selectedMode != value)
            {
                _selectedMode = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsValid { get; set; }

    public string Argument { get; set; } = string.Empty;

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
        if (sender is ComboBox selected && selected.SelectedItem != null)
        {
            // 先隐藏所有面板
            SetAllPanelsVisibility(PanelVisibilityConstants.Collapsed);

            // 根据操作模式显示面板
            switch (selected.SelectedItem)
            {
                case OperationMode.Delete:
                    HandleDeleteMode();
                    break;
                case OperationMode.RecycleBin:
                    HandleRecycleBinMode();
                    break;
                case OperationMode.Rename:
                    HandleRenameMode();
                    break;
                case OperationMode.UploadWebDAV:
                    HandleUploadWebDAVMode();
                    break;
                case OperationMode.Encryption:
                    HandleEncryptionMode();
                    break;
                case OperationMode.AISummary:
                    HandleAISummaryMode();
                    break;
                case OperationMode.AIClassification:
                    HandleAIClassificationMode();
                    break;
                case OperationMode.RunExternalPrograms:
                    ExcuteExternal();
                    break;
                default:
                    HandleDefaultMode();
                    break;
            }
        }
    }

    private void SetAllPanelsVisibility(Visibility visibility)
    {
        RenameButton.Visibility = visibility;
        TaskTargetPanel.Visibility = visibility;
        TaskSourcePanel.Visibility = visibility;
        EncryptedPanel.Visibility = visibility;
        TaskPromptPanel.Visibility = visibility;
        PromptPanel.Visibility = visibility;
        CustomPromptPanel.Visibility = visibility;
        TaskRulePanel.Visibility = visibility;
        ArgumentPanel.Visibility = visibility;
        RunTaskPanel.Visibility = visibility;
    }

    private void HandleDeleteMode()
    {
        TaskTargetPanel.Visibility = PanelVisibilityConstants.Visible;
        TaskRulePanel.Visibility = PanelVisibilityConstants.Visible;
        RunTaskPanel.Visibility = PanelVisibilityConstants.Collapsed;
    }

    private void HandleRecycleBinMode()
    {
        TaskTargetPanel.Visibility = PanelVisibilityConstants.Visible;
        TaskRulePanel.Visibility = PanelVisibilityConstants.Visible;
        RunTaskPanel.Visibility = PanelVisibilityConstants.Collapsed;
    }

    private void HandleRenameMode()
    {
        RenameButton.Visibility = PanelVisibilityConstants.Visible;
        TaskTargetTitle.Text = "NewNameAndPath".GetLocalized();
        TaskTargetPanel.Visibility = PanelVisibilityConstants.Visible;
        TaskSourcePanel.Visibility = PanelVisibilityConstants.Visible;
        TaskRulePanel.Visibility = PanelVisibilityConstants.Visible;
        RunTaskPanel.Visibility = PanelVisibilityConstants.Collapsed;
    }

    private void HandleUploadWebDAVMode()
    {
        TaskSourcePanel.Visibility = PanelVisibilityConstants.Visible;
        TaskRulePanel.Visibility = PanelVisibilityConstants.Visible;
        ViewModel.TaskTarget = ViewModel.TaskSource;
        RunTaskPanel.Visibility = PanelVisibilityConstants.Collapsed;
    }

    private void HandleEncryptionMode()
    {
        TaskSourcePanel.Visibility = PanelVisibilityConstants.Visible;
        TaskTargetPanel.Visibility = PanelVisibilityConstants.Visible;
        EncryptedPanel.Visibility = PanelVisibilityConstants.Visible;
        TaskRulePanel.Visibility = PanelVisibilityConstants.Visible;
        RunTaskPanel.Visibility = PanelVisibilityConstants.Collapsed;
    }

    private void HandleAISummaryMode()
    {
        TaskSourcePanel.Visibility = PanelVisibilityConstants.Visible;
        TaskTargetPanel.Visibility = PanelVisibilityConstants.Visible;
        TaskPromptPanel.Visibility = PanelVisibilityConstants.Visible;
        PromptPanel.Visibility = PanelVisibilityConstants.Visible;
        CustomPromptPanel.Visibility = PanelVisibilityConstants.Collapsed;
        TaskRulePanel.Visibility = PanelVisibilityConstants.Visible;
        RunTaskPanel.Visibility = PanelVisibilityConstants.Collapsed;
        if (SelectedMode == PromptType.BuiltIn)
        {
            PromptPanel.Visibility = PanelVisibilityConstants.Collapsed;
        }
    }

    private void HandleAIClassificationMode()
    {
        TaskSourcePanel.Visibility = PanelVisibilityConstants.Visible;
        TaskTargetPanel.Visibility = PanelVisibilityConstants.Visible;
        TaskPromptPanel.Visibility = PanelVisibilityConstants.Collapsed;
        PromptPanel.Visibility = PanelVisibilityConstants.Collapsed;
        CustomPromptPanel.Visibility = PanelVisibilityConstants.Visible;
        TaskRulePanel.Visibility = PanelVisibilityConstants.Collapsed;
        TaskRule = "*";
        RunTaskPanel.Visibility = PanelVisibilityConstants.Collapsed;
    }

    private void HandleDefaultMode()
    {
        TaskSourcePanel.Visibility = PanelVisibilityConstants.Visible;
        TaskTargetPanel.Visibility = PanelVisibilityConstants.Visible;
        TaskRulePanel.Visibility = PanelVisibilityConstants.Visible;
        ArgumentPanel.Visibility = PanelVisibilityConstants.Collapsed;
        RunTaskPanel.Visibility = PanelVisibilityConstants.Collapsed;
    }

    private void ExcuteExternal()
    {
        TaskTargetPanel.Visibility = PanelVisibilityConstants.Visible;
        RunTaskPanel.Visibility = PanelVisibilityConstants.Visible;
        TaskSourcePanel.Visibility = PanelVisibilityConstants.Collapsed;
        TaskRulePanel.Visibility = PanelVisibilityConstants.Collapsed;
        ArgumentPanel.Visibility = PanelVisibilityConstants.Visible;
        TaskRule = "*";
        TaskTargetTitle.Text = "工作目录";
    }

    private void RenameItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is PatternSnippetModel s)
        {
            RenameFlyout.Hide();
            Target.Text += "\\" + s.Code;
        }
    }

    private bool ValidateRuleString(string input)
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
        return IsValidRegex(input) && IsRegex;
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

    private void TaskGroupNameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var text = (sender as TextBox)?.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            ValidTextBlock.Visibility = Visibility.Visible;
            ValidTextBlock.Text = "GroupInformationVerificationAdd".GetLocalized() + "\n" + "GroupInformationVerification".GetLocalized();
            IsValid = false;
        }
        else
        {
            ValidTextBlock.Visibility = Visibility.Collapsed;
            IsValid = true;
        }
    }

    private void TaskRuleBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var text = (sender as TextBox)?.Text;
        IsValid = ValidateRuleString(text);
        if (!IsValid && !string.IsNullOrWhiteSpace(text))
        {
            // 验证未通过显示错误信息
            TaskRuleBoxValid.Visibility = Visibility.Visible;
            TaskRuleBoxValid.Text = "ValidRuleText".GetLocalized();
        }
        else
        {
            // 隐藏错误提示框
            TaskRuleBoxValid.Visibility = Visibility.Collapsed;
        }
    }

    private void TaskGroupNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var text = (sender as TextBox)?.Text;
        IsValid = ValidGroupName(text);
        if (!IsValid)
        {
            ValidTextBlock.Visibility = Visibility.Visible;
            ValidTextBlock.Text = "GroupInformationVerificationAdd".GetLocalized() + "\n" + "GroupInformationVerification".GetLocalized();
        }
        else
        {
            ValidTextBlock.Visibility = Visibility.Collapsed;
        }
    }

    private void TaskRuleBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var text = (sender as TextBox)?.Text;
        IsValid = ValidateRuleString(text);
        if (!IsValid)
        {
            // 验证未通过显示错误信息
            TaskRuleBoxValid.Visibility = Visibility.Visible;
            TaskRuleBoxValid.Text = "ValidRuleText".GetLocalized();
        }
        else
        {
            // 隐藏错误提示框
            TaskRuleBoxValid.Visibility = Visibility.Collapsed;
        }
    }

    private bool ValidGroupName(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }
        return true;
    }

    private void RadioButton_Checked(object sender, RoutedEventArgs e)
    {
        var radioButton = sender as RadioButton;
        if (radioButton != null)
        {
            if (radioButton.Name.Equals("BuiltIn"))
            {
                PromptPanel.Visibility = PanelVisibilityConstants.Collapsed;
            }
            else if (radioButton.Name.Equals("Custom"))
            {
                PromptPanel.Visibility = PanelVisibilityConstants.Visible;
            }
        }
    }

    private void DialogFilterListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        FilterButtonTeachingTip.IsOpen = false;
        ViewModel.ItemClickChangedCommand.Execute(sender);
    }
}

public static class PanelVisibilityConstants
{
    public static readonly Visibility Collapsed = Visibility.Collapsed;
    public static readonly Visibility Visible = Visibility.Visible;
}

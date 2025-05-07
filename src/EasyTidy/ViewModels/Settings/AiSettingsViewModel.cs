using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using EasyTidy.Common.Database;
using EasyTidy.Common.Model;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using EasyTidy.Service.AIService;
using EasyTidy.Views.ContentDialogs;
using MathNet.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.ViewModels;

public partial class AiSettingsViewModel : ObservableObject
{
    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    [ObservableProperty]
    private ObservableCollection<AIServiceTable> _aiList;

    [ObservableProperty]
    private AdvancedCollectionView _aiListACV;

    private readonly AppDbContext _dbContext;

    [ObservableProperty]
    private IThemeSelectorService _themeSelectorService;

    public AiSettingsViewModel(IThemeSelectorService themeSelectorService)
    {
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new("Settings".GetLocalized(), typeof(SettingsViewModel).FullName!),
            new("AiSettingPage_Header".GetLocalized(), typeof(AiSettingsViewModel).FullName!),
        };
        _dbContext = App.GetService<AppDbContext>();
        _themeSelectorService = themeSelectorService;
    }

    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    [ObservableProperty]
    private IList<ServiceType> _serviceTypes = Enum.GetValues(typeof(ServiceType)).Cast<ServiceType>().ToList();

    [ObservableProperty]
    private ServiceType _serviceType = ServiceType.OpenAI;

    [ObservableProperty]
    private ServiceType _selectedServiceType = ServiceType.OpenAI;

    [ObservableProperty]
    private DefaultChatModel _defaultChatModel;

    [ObservableProperty]
    private ObservableCollection<DefaultChatModel> _defaultChatModels;

    [RelayCommand]
    private async Task OnPageLoaded()
    {
        try
        {
            var aiList = await _dbContext.AIService.ToListAsync();
            var observableAiList = new ObservableCollection<AIServiceTable>(aiList);
            var acv = new AdvancedCollectionView(observableAiList);

            var defaultChatModels = new ObservableCollection<DefaultChatModel>();
            foreach (var item in aiList.Where(x => x.IsEnabled == true))
            {
                defaultChatModels.Add(new DefaultChatModel()
                {
                    ServiceType = item.Type,
                    ModelName = item.Name,
                    Identifier = item.Identify,
                    DisplayName = item.Name + " (" + EnumHelper.GetDisplayName(item.Type) + ")"
                });
            }

            // 获取默认的 AI 条目
            var defaultChatModelEntry = aiList.FirstOrDefault(x => x.IsDefault);

            // ⚠ 使用集合中的现有对象作为 SelectedItem 的引用
            var defaultChatModel = defaultChatModels.FirstOrDefault(m =>
                m.Identifier == defaultChatModelEntry?.Identify &&
                m.ServiceType == defaultChatModelEntry.Type);

            // 分发到 UI 线程更新绑定属性
            dispatcherQueue.TryEnqueue(() =>
            {
                AiList = observableAiList;
                AiListACV = acv;
                // ServiceType = defaultChatModelEntry.Type;
                DefaultChatModels = defaultChatModels;
                DefaultChatModel = defaultChatModel;
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"AiSettingsViewModel: OnPageLoaded 异常信息 {ex}");
        }
    }

    [RelayCommand]
    private async Task AddAIClick(object sender)
    {
        var dialog = new AddAIContentDialog
        {
            Title = "AddCustomChatModel".GetLocalized(),

        };
        dialog.CloseButtonText = "CancelText".GetLocalized();
        dialog.PrimaryButtonText = "SaveText".GetLocalized();
        dialog.SecondaryButtonText = "VerifyText".GetLocalized();

        dialog.PrimaryButtonClick += OnAddAIPrimaryButton;

        await dialog.ShowAsync();
    }

    private async void OnAddAIPrimaryButton(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            var dialog = sender as AddAIContentDialog;
            dialog.ValidateProperty(dialog.ChatModel, "ChatModel");
            if (dialog.HasErrors)
            {
                // 阻止对话框关闭
                args.Cancel = true;
                return;
            }
            string baseUrl = dialog.BaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                AIServiceFactory aIServiceFactory = new AIServiceFactory();
                baseUrl = aIServiceFactory.GetService(dialog.ServiceType);
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                Logger.Warn("AI服务的 BaseUrl 为空，操作已终止。");
                return;
            }

            var aiService = new AIServiceTable
            {
                Name = dialog.ModelName,
                Identify = Guid.NewGuid(),
                Type = dialog.ServiceType,
                IsEnabled = true,
                Url = baseUrl,
                AppKey = dialog.AppKey,
                AppID = dialog.AppID,
                Model = dialog.ChatModel,
                Temperature = Math.Round(Math.Clamp(dialog.Temperature, 0, 2), 2, MidpointRounding.AwayFromZero),
                IsDefault = false
            };
            await _dbContext.AIService.AddAsync(aiService);
            await _dbContext.SaveChangesAsync();
            await OnPageLoaded();
        }
        catch (Exception ex)
        {
            Logger.Error($"AiSettingsViewModel: OnAddAIPrimaryButton 异常信息 {ex}");
        }
    }

    [RelayCommand]
    private async Task OnSetDefaultClick(object sender)
    {
        if (DefaultChatModel == null || DefaultChatModel.Identifier == Guid.Empty)
            return;

        var newDefaultId = DefaultChatModel.Identifier;

        // 查找目标 AI 实体
        var newDefaultAi = await _dbContext.AIService
            .FirstOrDefaultAsync(x => x.Identify.ToString().ToLower().Equals(newDefaultId.ToString().ToLower()));

        if (newDefaultAi == null)
        {
            Logger.Warn($"未找到匹配的 AI 服务，Identifier: {newDefaultId}");
            return;
        }

        // 查询当前已有的默认项（若有）
        var oldDefaults = await _dbContext.AIService
            .Where(x => x.IsDefault && x.Identify != newDefaultId)
            .ToListAsync();

        // 取消所有旧默认
        foreach (var old in oldDefaults)
        {
            old.IsDefault = false;
        }

        // 更新所有引用旧默认值的任务引用
        var taskList = await _dbContext.TaskOrchestration
            .Where(x => x.AIIdentify != Guid.Empty)
            .ToListAsync();

        foreach (var task in taskList)
        {
            task.AIIdentify = newDefaultId;
        }

        // 设置新的默认项
        newDefaultAi.IsDefault = true;

        // 保存所有更改
        await _dbContext.SaveChangesAsync();

        // 刷新页面绑定
        await OnPageLoaded();
    }

    [RelayCommand]
    private async Task OnDeleteAIServiceClick(object sender)
    {
        if (sender is Button button && button.DataContext is AIServiceTable aiService)
        {
            var dialog = new ContentDialog
            {
                Title = "DeleteAIService".GetLocalized(),
                Content = "SureDelete".GetLocalized(),
                CloseButtonText = "CancelText".GetLocalized(),
                PrimaryButtonText = "DeleteText".GetLocalized(),
                XamlRoot = App.MainWindow.Content.XamlRoot,
                RequestedTheme = ThemeSelectorService.Theme,
            };
            dialog.PrimaryButtonClick += async (s, args) =>
            {
                _dbContext.AIService.Remove(aiService);
                // 删除AI引用
                var taskList = await _dbContext.TaskOrchestration.Where(x => x.AIIdentify == aiService.Identify)
                    .ToListAsync();
                foreach (var task in taskList)
                {
                    task.AIIdentify = Guid.Empty;
                }
                await _dbContext.SaveChangesAsync();
                await OnPageLoaded();
            };
            await dialog.ShowAsync();
        }
    }

    [RelayCommand]
    private async Task OnUpdateAIServiceClick(object sender)
    {
        if (sender is Button button && button.DataContext is AIServiceTable aiService)
        {
            var dialog = new AddAIContentDialog
            {
                Title = "UpdateAIService".GetLocalized(),
                ModelName = aiService.Name,
                AppKey = aiService.AppKey,
                AppID = aiService.AppID,
                BaseUrl = aiService.Url,
                ChatModel = aiService.Model,
                Temperature = aiService.Temperature,
                ServiceType = aiService.Type
            };
            dialog.CloseButtonText = "CancelText".GetLocalized();
            dialog.PrimaryButtonText = "SaveText".GetLocalized();
            dialog.SecondaryButtonText = "VerifyText".GetLocalized();
            dialog.PrimaryButtonClick += async (s, args) =>
            {
                aiService.Name = dialog.ModelName;
                aiService.AppKey = dialog.AppKey;
                aiService.AppID = dialog.AppID;
                aiService.Url = dialog.BaseUrl;
                aiService.Model = dialog.ChatModel;
                aiService.Temperature = Math.Round(Math.Clamp(dialog.Temperature, 0, 2), 2, MidpointRounding.AwayFromZero);
                aiService.Type = dialog.ServiceType;

                await _dbContext.SaveChangesAsync();
                await OnPageLoaded();
            };
            await dialog.ShowAsync();
        }
    }

    [RelayCommand]
    private async Task OnChangeIsEnabled(object sender)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is AIServiceTable aiService)
        {
            aiService.IsEnabled = checkBox.IsChecked == true;
            await _dbContext.SaveChangesAsync();
            await OnPageLoaded();
        }
    }

    public async Task<(string, bool)> VerifyServiceAsync(object sender)
    {
        bool isValid = false;
        string result = string.Empty;
        var token = new CancellationToken();
        try
        {
            var dialog = sender as AddAIContentDialog;
            string baseUrl = dialog.BaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                AIServiceFactory aIServiceFactory = new();
                baseUrl = aIServiceFactory.GetService(dialog.ServiceType);
            }
            if (string.IsNullOrEmpty(dialog.ChatModel))
            {
                return ("VerifyModel".GetLocalized(), isValid);
            }
            var aiService = new AIServiceTable
            {
                Name = dialog.ModelName,
                Identify = Guid.NewGuid(),
                Type = dialog.ServiceType,
                IsEnabled = true,
                Url = baseUrl,
                AppKey = dialog.AppKey,
                AppID = dialog.AppID,
                Model = dialog.ChatModel,
                Temperature = Math.Round(Math.Clamp(dialog.Temperature, 0, 2), 2, MidpointRounding.AwayFromZero),
                IsDefault = false
            };
            var llm = AIServiceFactory.CreateAIServiceLlm(aiService);
            var reqModel = new RequestModel("你是谁", "ZH-CN");
            await llm.PredictAsync(reqModel, _ => result = "VerifySucess".GetLocalized(), token);
            isValid = true;
        }
        catch (Exception ex)
        {
            Logger.Error($"AiSettingsViewModel: VerifyService 异常信息 {ex}");
            return (ex.Message, isValid);
        }
        return (result, isValid);
    }
}

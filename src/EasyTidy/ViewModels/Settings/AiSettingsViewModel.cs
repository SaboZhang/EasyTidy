using CommunityToolkit.WinUI;
using EasyTidy.Common.Model;
using EasyTidy.Model;
using EasyTidy.Service.AIService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.ViewModels;

public class AiSettingsViewModel : ObservableObject
{
    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public AiSettingsViewModel()
    {
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new("Settings".GetLocalized(), typeof(SettingsViewModel).FullName!),
            new("AiSettingPage_Header".GetLocalized(), typeof(AiSettingsViewModel).FullName!),
        };
    }

    private IAIServiceLlm CreateAIServiceLlm(AIServiceTable entity)
    {
        IAIServiceLlm aIServiceLlm = entity.Type switch
        {
            ServiceType.OpenAI => new OpenAIService(),
            _ => throw new NotImplementedException(),
        };

        if (!string.IsNullOrEmpty(entity.UserDefinePromptsJson))
        {
            aIServiceLlm.UserDefinePrompts = JsonConvert.DeserializeObject<List<UserDefinePrompt>>(entity.UserDefinePromptsJson) ?? new List<UserDefinePrompt>();
        }

        // 反射填充属性
        var properties = entity.GetType().GetProperties();
        foreach (var prop in properties)
        {
            var translatorProp = aIServiceLlm.GetType().GetProperty(prop.Name);
            if (translatorProp != null && translatorProp.CanWrite)
            {
                translatorProp.SetValue(aIServiceLlm, prop.GetValue(entity));
            }
        }

        return aIServiceLlm;
    }
}

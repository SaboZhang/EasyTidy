using EasyTidy.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Service.AIService;

public class AIServiceFactory
{
    private readonly Dictionary<ServiceType, string> _services;

    public AIServiceFactory()
    {
        _services = new Dictionary<ServiceType, string>
        {
            { ServiceType.OpenAI, "https://api.openai.com" },
            { ServiceType.Ollama, "http://localhost:11434" },
            { ServiceType.QWen, "https://dashscope.aliyuncs.com" },
            { ServiceType.HuggingFace, "https://router.huggingface.co" },
            { ServiceType.Gemini, "https://generativelanguage.googleapis.com" },
            { ServiceType.DeepSeek, "https://api.deepseek.com" },
            { ServiceType.Claude, "https://api.anthropic.com" },
            { ServiceType.WenXin, "https://aip.baidubce.com/rpc/2.0/ai_custom/v1" },
            { ServiceType.Azure, "https://docs-test-001.openai.azure.com" },
            { ServiceType.Hunyuan, "https://hunyuan.tencentcloudapi.com" },
            { ServiceType.Volcengine, "https://ark.cn-beijing.volces.com" },
            { ServiceType.OpenAIFormat, string.Empty },
        };
    }

    public string GetService(ServiceType provider)
    {
        return _services.TryGetValue(provider, out var service)
            ? service
            : throw new Exception("未知的 AI 服务商");
    }

    public static IAIServiceLlm CreateAIServiceLlm(AIServiceTable entity, string prompt = null)
    {
        IAIServiceLlm aIServiceLlm = entity.Type switch
        {
            ServiceType.OpenAI => new OpenAIService(),
            ServiceType.QWen => new QWenService(),
            ServiceType.Ollama => new OllamaService(),
            ServiceType.HuggingFace => new HuggingFaceService(),
            ServiceType.OpenAIFormat => new OpenAICompatibleService(),
            ServiceType.Gemini => new GeminiService(),
            ServiceType.DeepSeek => new DeepSeekService(),
            ServiceType.Claude => new ClaudeService(),
            ServiceType.WenXin => new BaiduBceService(),
            ServiceType.Azure => new AzureOpenAIService(),
            ServiceType.Hunyuan => new HunyuanService(),
            ServiceType.Volcengine => new VolcengineService(),
            _ => throw new NotImplementedException(),
        };

        if (!string.IsNullOrEmpty(prompt))
        {
            aIServiceLlm.UserDefinePrompts = JsonConvert.DeserializeObject<List<UserDefinePrompt>>(prompt) ?? new List<UserDefinePrompt>();
        }

        // 反射填充属性
        var properties = entity.GetType().GetProperties();
        foreach (var prop in properties)
        {
            var easyTidyProp = aIServiceLlm.GetType().GetProperty(prop.Name);
            if (easyTidyProp != null && easyTidyProp.CanWrite)
            {
                easyTidyProp.SetValue(aIServiceLlm, prop.GetValue(entity));
            }
        }

        return aIServiceLlm;
    }
}

using EasyTidy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Service.AIService;

public class AIServiceFactory
{
    private readonly Dictionary<ServiceType, IAIService> _services;

    public AIServiceFactory()
    {
        _services = new Dictionary<ServiceType, IAIService>
            {
                { ServiceType.OpenAI, null },
                { ServiceType.Ollama, null },
                { ServiceType.QWen, null }
            };
    }

    public IAIService GetService(ServiceType provider)
    {
        return _services.TryGetValue(provider, out var service)
            ? service
            : throw new Exception("未知的 AI 服务商");
    }
}

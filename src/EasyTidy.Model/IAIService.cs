using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasyTidy.Model;

public interface IAIService
{
    Guid Identify { get; set; }

    ServiceType Type { get; set; }

    string Name { get; set; }

    bool IsEnabled { get; set; }

    string Url { get; set; }

    string AppID { get; set; }

    string AppKey { get; set; }

    string Model { get; set; }

    ServiceResult Data { get; set; }

    List<UserDefinePrompt> UserDefinePrompts { get; set; }

    Task<ServiceResult> PredictAsync(object request, CancellationToken token);

    Task PredictAsync(object request, Action<string> onDataReceived, CancellationToken token);
}

public interface IAIServiceLlm : IAIService
{
    double Temperature { get; set; }

    bool IsDefault { get; set; }
}

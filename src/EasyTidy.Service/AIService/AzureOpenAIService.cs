using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EasyTidy.Service.AIService;

public partial class AzureOpenAIService : LLMServiceBase, IAIServiceLlm
{
    public AzureOpenAIService() : this(Guid.NewGuid(), "https://docs-test-001.openai.azure.com", "Azure OpenAI") { }
    public AzureOpenAIService(
        Guid identify,
        string url,
        string name = "",
        ServiceType type = ServiceType.Azure,
        string appID = "", string appKey = "",
        bool isEnabled = true,
        string model = ""
        )
    {
        Identify = identify;
        Url = url;
        Name = name;
        Type = type;
        AppID = appID;
        AppKey = appKey;
        IsEnabled = isEnabled;
        Model = model;
    }

    public Task<ServiceResult> PredictAsync(object request, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public async Task PredictAsync(object request, Action<string> onDataReceived, CancellationToken token)
    {
        if (string.IsNullOrEmpty(Url) || string.IsNullOrEmpty(AppKey))
            throw new Exception("请先完善配置");

        if (request is not RequestModel req)
            throw new Exception($"请求数据出错: {request}");

        var source = req.Text;
        var language = req.Language;

        UriBuilder uriBuilder = new(Url);

        // 选择模型
        var a_model = Model.Trim();
        a_model = string.IsNullOrEmpty(a_model) ? "gpt35" : a_model;

        var path = $"/openai/deployments/{a_model}/chat/completions";
        if (uriBuilder.Path == "/")
            uriBuilder.Path = path;

        if (string.IsNullOrEmpty(uriBuilder.Query))
        {
            uriBuilder.Query = "?api-version=2024-02-01";
        }

        // 替换Prompt关键字
        var a_messages =
            (UserDefinePrompts.FirstOrDefault(x => x.Enabled)?.Prompts ?? throw new Exception("请先完善Propmpt配置")).Clone();
        a_messages.ToList().ForEach(item =>
            item.Content = item.Content.Replace("$source", source).Replace("$content", language));

        // 温度限定
        var a_temperature = Math.Clamp(Temperature, 0, 2);

        // 构建请求数据
        var reqData = new
        {
            messages = a_messages,
            temperature = a_temperature,
            stream = true
        };

        var header = new Dictionary<string, string>
        {
            { "api-key", AppKey }
        };
        var jsonData = Json.SerializeForModel(reqData, PropertyCase.CamelCase);

        try
        {
            await HttpUtil.PostAsync(
                uriBuilder.Uri,
                header,
                jsonData,
                msg =>
                {
                    if (string.IsNullOrEmpty(msg?.Trim()))
                        return;

                    var preprocessString = msg.Replace("data:", "").Trim();

                    // 结束标记
                    if (preprocessString.Equals("[DONE]"))
                        return;

                    // 解析JSON数据
                    var parsedData = JsonConvert.DeserializeObject<JObject>(preprocessString);

                    if (parsedData is null)
                        return;

                    // 提取content的值
                    var contentValue = parsedData["choices"]?.FirstOrDefault()?["delta"]?["content"]?.ToString();

                    if (string.IsNullOrEmpty(contentValue))
                        return;

                    onDataReceived?.Invoke(contentValue);
                },
                token
            ).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == null)
        {
            var msg = $"请检查服务是否可以正常访问: {Name} ({Url}).";
            throw new HttpRequestException(msg);
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            if (ex.InnerException is { } innEx)
            {
                var innMsg = JsonConvert.DeserializeObject<JObject>(innEx.Message);
                msg += $" {innMsg?["error"]?["message"]}";
                LogService.Logger.Error($"({Name})({Identify}) raw content:\n{innEx.Message}");
            }

            msg = msg.Trim();

            throw new Exception(msg);
        }
    }
}

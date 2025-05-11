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

public partial class ClaudeService : LLMServiceBase, IAIServiceLlm
{
    public ClaudeService() : this(Guid.NewGuid(), "https://api.anthropic.com", "Claude") { }

    public ClaudeService(
        Guid identify,
        string url,
        string name = "",
        ServiceType type = ServiceType.Claude,
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

        if (uriBuilder.Path == "/")
            uriBuilder.Path = "/v1/messages";

        // 选择模型
        var a_model = Model.Trim();
        a_model = string.IsNullOrEmpty(a_model) ? "claude-3-5-sonnet-20240620" : a_model;

        // 温度限定
        var a_temperature = Math.Clamp(Temperature, 0, 1);

        // 替换Prompt关键字
        var a_messages =
            (UserDefinePrompts.FirstOrDefault(x => x.Enabled)?.Prompts ?? throw new Exception("请先完善Propmpt配置")).Clone();
        a_messages.ToList().ForEach(item =>
            item.Content = item.Content.Replace("$source", source).Replace("$content", language));

        var systemMsg =
            a_messages.FirstOrDefault(x => x.Role.Equals("system", StringComparison.CurrentCultureIgnoreCase));

        //https://docs.anthropic.com/en/docs/build-with-claude/prompt-engineering/system-prompts#how-to-give-claude-a-role
        object reqData;
        if (systemMsg != null)
        {
            a_messages.Remove(systemMsg);

            reqData = new
            {
                model = a_model,
                messages = a_messages,
                system = systemMsg.Content,
                temperature = a_temperature,
                max_tokens = 4096,
                stream = true
            };
        }
        else
        {
            reqData = new
            {
                model = a_model,
                messages = a_messages,
                temperature = a_temperature,
                max_tokens = 4096,
                stream = true
            };
        }

        var jsonData = Json.SerializeForModel(reqData, PropertyCase.CamelCase);

        var headers = new Dictionary<string, string>
        {
            { "x-api-key", AppKey },
            { "anthropic-version", "2023-06-01" }
        };

        try
        {
            await HttpUtil.PostAsync(
                uriBuilder.Uri,
                headers,
                jsonData,
                msg =>
                {
                    if (string.IsNullOrEmpty(msg?.Trim()) || msg.StartsWith("event"))
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
                    var contentValue = parsedData["delta"]?["text"]?.ToString();

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

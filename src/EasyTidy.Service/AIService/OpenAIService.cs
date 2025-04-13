using CommunityToolkit.Mvvm.ComponentModel;
using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EasyTidy.Service.AIService;

public partial class OpenAIService : ObservableObject, IAIServiceLlm
{
    public OpenAIService()
        : this(Guid.NewGuid(), "https://api.openai.com", "OpenAI")
    { }

    public OpenAIService(
        Guid identify, 
        string url, 
        string name = "", 
        ServiceType type = ServiceType.OpenAI, 
        string appID = "", string appKey = "", 
        bool isEnabled = true, 
        string model = "gpt-3.5-turbo")
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

    [ObservableProperty] 
    private Guid _identify = Guid.Empty;
    [ObservableProperty] 
    private ServiceType _type = 0;
    [ObservableProperty] 
    private double _temperature = 0.8;
    [ObservableProperty] 
    private bool _isEnabled = true;
    [ObservableProperty] 
    private string _name = string.Empty;
    [ObservableProperty]
    private string _url = string.Empty;
    [ObservableProperty]
    private string _appID = string.Empty;
    [ObservableProperty]
    private string _appKey = string.Empty;
    [ObservableProperty]
    private ServiceResult _data = ServiceResult.Reset;
    [ObservableProperty]
    private string _model = "gpt-3.5-turbo";
    [ObservableProperty]
    private List<UserDefinePrompt> _userDefinePrompts = 
    [
        new UserDefinePrompt(
            "总结",
            [
                new Prompt("system", "You are a text summarizer, you can only summarize the text, never interpret it."),
                new Prompt("user", "Summarize the following text in $source: $content")
            ],
            true
        ),
        new UserDefinePrompt(
            "分类",
            [
                new Prompt("system", "You are a document classification expert who can categorize files based on their names."),
                new Prompt("user", "Please classify these $source as: $target; Output in JSON format.")
            ]
        ),
    ];

    [ObservableProperty]
    private bool _isDefault = true;

    public async Task PredictAsync(object request, Action<string> onDataReceived, CancellationToken token)
    {
        if (string.IsNullOrEmpty(Url) /* || string.IsNullOrEmpty(AppKey)*/)
            throw new Exception("请先完善配置");

        if (request is not RequestModel req)
            throw new Exception($"请求数据出错: {request}");

        var source = req.Text;
        UriBuilder uriBuilder = new(Url);

        // 兼容旧版API: https://platform.openai.com/docs/guides/text-generation
        if (!uriBuilder.Path.EndsWith("/v1/chat/completions") && !uriBuilder.Path.EndsWith("/v1/completions"))
            uriBuilder.Path = "/v1/chat/completions";

        // 选择模型
        var a_model = Model.Trim();
        a_model = string.IsNullOrEmpty(a_model) ? "gpt-3.5-turbo" : a_model;

        // 替换Prompt关键字
        var a_messages =
            (UserDefinePrompts.FirstOrDefault(x => x.Enabled)?.Prompts ?? throw new Exception("请先完善Propmpt配置")).Clone();
        a_messages.ToList().ForEach(item =>
            item.Content = item.Content.Replace("$source", source).Replace("$target", "")
                .Replace("$content", "zh-CN"));

        // 温度限定
        var a_temperature = Math.Clamp(Temperature, 0, 2);

        // 构建请求数据
        var reqData = new
        {
            model = a_model,
            messages = a_messages,
            temperature = a_temperature,
            stream = true
        };

        var jsonData = JsonConvert.SerializeObject(reqData);

        try
        {
            await HttpUtil.PostAsync(
                uriBuilder.Uri,
                jsonData,
                AppKey,
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
            var msg = $"请检查服务是否可以正常访问: ({Url}).";
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
                LogService.Logger.Error($"({Identify}) raw content:\n{innEx.Message}");
            }

            msg = msg.Trim();

            throw new Exception(msg);
        }
    }

    public Task<ServiceResult> PredictAsync(object request, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}

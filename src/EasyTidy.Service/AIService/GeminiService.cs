using CommunityToolkit.Mvvm.ComponentModel;
using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Util;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace EasyTidy.Service.AIService;

public partial class GeminiService : ObservableObject, IAIServiceLlm
{
    public GeminiService() : this(Guid.NewGuid(), "https://generativelanguage.googleapis.com", "Gemini") { }

    public GeminiService(Guid identify,
        string url,
        string name = "",
        ServiceType type = ServiceType.Gemini,
        string appID = "", string appKey = "",
        bool isEnabled = true,
        string model = "gemini-2.0-flash")
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
    private string _model = "gemini-2.0-flash";
    [ObservableProperty]
    private List<UserDefinePrompt> _userDefinePrompts =
    [
        new UserDefinePrompt(
            "总结",
            [
                new Prompt("system", "You are a text summarizer, you can only summarize the text, never interpret it."),
                new Prompt("user", "Summarize the following text in $source; language: $content")
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
        a_model = string.IsNullOrEmpty(a_model) ? "gemini-pro" : a_model;

        if (!uriBuilder.Path.EndsWith($"/v1beta/models/{a_model}:streamGenerateContent"))
            uriBuilder.Path = $"/v1beta/models/{a_model}:streamGenerateContent";

        uriBuilder.Query = $"key={AppKey}";

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
            contents = a_messages.Select(e => new { role = e.Role, parts = new[] { new { text = e.Content } } }),
            generationConfig = new { temperature = a_temperature },
            safetySettings = new[]
            {
                new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE"},         //骚扰内容。
                new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE"},        //仇恨言论和内容。
                new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE"},  //露骨色情内容。
                new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE"},  //危险内容。
            }
        };

        // 为了流式输出与MVVM还是放这里吧
        var jsonData = JsonConvert.SerializeObject(reqData);

        try
        {
            await HttpUtil.PostAsync(
                uriBuilder.Uri,
                jsonData,
                null,
                msg =>
                {
                    // 使用正则表达式提取目标字符串
                    var pattern = "(?<=\"text\": \")[^\"]+(?=\")";

                    var match = Regex.Match(msg, pattern);

                    if (match.Success) onDataReceived?.Invoke(match.Value.Replace("\\n", "\n"));
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
                var innMsg = JsonConvert.DeserializeObject<JArray>(innEx.Message);
                msg += $" {innMsg?.FirstOrDefault()?["error"]?["message"]}";
                LogService.Logger.Error($"({Name})({Identify}) raw content:\n{innEx.Message}");
            }

            msg = msg.Trim();

            throw new Exception(msg);
        }
    }
}

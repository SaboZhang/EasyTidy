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

namespace EasyTidy.Service.AIService;

public partial class HuggingFaceService : ObservableObject, IAIServiceLlm
{
    public HuggingFaceService() : this(Guid.NewGuid(), "https://api-inference.huggingface.co", "HuggingFace") { }

    public HuggingFaceService(
        Guid identify,
        string url,
        string name = "",
        ServiceType type = ServiceType.HuggingFace,
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
            throw new Exception("请先完善 Hugging Face 配置");

        if (request is not RequestModel req)
            throw new Exception($"请求数据出错: {request}");

        var input = req.Text;
        var language = req.Language;

        UriBuilder uriBuilder = new(Url);

        // 选择模型名称
        var modelId = Model.Trim();
        modelId = string.IsNullOrEmpty(modelId) ? "meta-llama/Llama-2-7b-chat-hf" : modelId;

        if (!uriBuilder.Path.EndsWith($"/models/{modelId}"))
            uriBuilder.Path = $"/models/{modelId}";

        // 替换Prompt关键字
        var promptTemplate =
            (UserDefinePrompts.FirstOrDefault(x => x.Enabled)?.Prompts ?? throw new Exception("请先完善Prompt配置")).Clone();

        promptTemplate.ToList().ForEach(item =>
            item.Content = item.Content.Replace("$source", input).Replace("$content", language));

        var finalPrompt = string.Join("\n", promptTemplate.Select(p => p.Content));

        // 温度
        var a_temperature = Math.Clamp(Temperature, 0, 2);

        var reqData = new
        {
            inputs = finalPrompt,
            parameters = new
            {
                temperature = a_temperature,
                return_full_text = false,
                do_sample = true,
                top_p = 0.95,
                max_new_tokens = 1024
            },
            options = new
            {
                wait_for_model = true,
                use_cache = false
            }
        };

        var jsonData = Json.SerializeForModel(reqData, PropertyCase.CamelCase);

        try
        {
            await HttpUtil.PostAsync(
                uriBuilder.Uri,
                jsonData,
                $"Bearer {AppKey}",
                msg =>
                {
                    if (string.IsNullOrWhiteSpace(msg))
                        return;

                    try
                    {
                        // HF接口可能返回多个候选结果
                        var predictions = JsonConvert.DeserializeObject<JArray>(msg);

                        if (predictions is null || predictions.Count == 0)
                            return;

                        var content = predictions[0]?["generated_text"]?.ToString();
                        if (!string.IsNullOrEmpty(content))
                        {
                            onDataReceived?.Invoke(content);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Logger.Error($"({Name})({Identify}) JSON解析失败: {ex.Message}");
                    }
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
            var msg = $"请检查 Hugging Face 服务是否可以访问: {Name} ({Url})。";
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
                msg += $" {innMsg?["error"]}";
                LogService.Logger.Error($"({Name})({Identify}) raw content:\n{innEx.Message}");
            }

            msg = msg.Trim();
            throw new Exception(msg);
        }
    }
}

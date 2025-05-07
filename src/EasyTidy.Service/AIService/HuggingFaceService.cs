using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EasyTidy.Service.AIService;

public partial class HuggingFaceService : LLMServiceBase, IAIServiceLlm
{
    public HuggingFaceService() : this(Guid.NewGuid(), "https://router.huggingface.co", "HuggingFace") { }

    public HuggingFaceService(
        Guid identify,
        string url,
        string name = "",
        ServiceType type = ServiceType.HuggingFace,
        string appID = "", string appKey = "",
        bool isEnabled = true,
        string model = "")
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
            throw new Exception("请先完善 Hugging Face 配置");

        if (request is not RequestModel req)
            throw new Exception($"请求数据出错: {request}");

        var input = req.Text;
        var language = req.Language;

        UriBuilder uriBuilder = new(Url);

        // 选择模型名称
        var modelId = Model.Trim();
        modelId = string.IsNullOrEmpty(modelId) ? "meta-llama/Llama-2-7b-chat-hf" : modelId;

        if (!uriBuilder.Path.EndsWith($"/hf-inference/models/{modelId}/v1/chat/completions"))
            uriBuilder.Path = $"/hf-inference/models/{modelId}/v1/chat/completions";

        // 替换Prompt关键字
        var promptTemplate =
            (UserDefinePrompts.FirstOrDefault(x => x.Enabled)?.Prompts ?? throw new Exception("请先完善Prompt配置")).Clone();

        promptTemplate.ToList().ForEach(item =>
            item.Content = item.Content.Replace("$source", input).Replace("$content", language));

        // 温度
        var a_temperature = Math.Clamp(Temperature, 0, 2);

        var reqData = new
        {
            model = modelId,
            messages = promptTemplate,
            temperature = a_temperature,
            stream = true
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

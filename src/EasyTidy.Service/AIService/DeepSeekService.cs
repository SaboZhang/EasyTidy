using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasyTidy.Service.AIService;

public partial class DeepSeekService : LLMServiceBase, IAIServiceLlm
{
    public DeepSeekService() : this(Guid.NewGuid(), "https://api.deepseek.com", "DeepSeek") { }

    public DeepSeekService(Guid identify,
        string url,
        string name = "",
        ServiceType type = ServiceType.DeepSeek,
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
            throw new Exception("请先完善配置");

        if (request is not RequestModel req)
            throw new Exception($"请求数据出错: {request}");

        var source = req.Text;
        var language = req.Language;

        UriBuilder uriBuilder = new(Url);

        if (uriBuilder.Path == "/")
            uriBuilder.Path = "/chat/completions";

        // 选择模型
        var a_model = Model.Trim();
        a_model = string.IsNullOrEmpty(a_model) ? "deepseek-chat" : a_model;

        // 替换Prompt关键字
        var a_messages =
            (UserDefinePrompts.FirstOrDefault(x => x.Enabled)?.Prompts ?? throw new Exception("请先完善Propmpt配置")).Clone();
        a_messages.ToList().ForEach(item =>
            item.Content = item.Content.Replace("$source", source).Replace("$content", language));

        // 温度限定
        var a_temperature = Math.Clamp(Temperature, 0, 1);

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
            var sb = new StringBuilder();
            bool isThink = false;

            await HttpUtil.PostAsync(
                uriBuilder.Uri,
                jsonData,
                AppKey,
                msg =>
                {
                    if (string.IsNullOrEmpty(msg?.Trim()) || msg.StartsWith("event"))
                        return;

                    var preprocessString = msg.Replace("data:", "").Trim();

                    // 结束标记
                    if (preprocessString.Equals("[DONE]"))
                        return;

                    try
                    {
                        // 解析JSON数据
                        var parsedData = JsonConvert.DeserializeObject<JObject>(preprocessString);

                        if (parsedData is null)
                            return;

                        // 提取content的值
                        var contentValue = parsedData["choices"]?.FirstOrDefault()?["delta"]?["content"]?.ToString();

                        if (string.IsNullOrEmpty(contentValue))
                            return;

                        /***********************************************************************
                         * 推理模型思考内容
                         * 1. content字段内：Groq（推理后带有换行）
                         * 2. reasoning_content字段内：DeepSeek、硅基流动（推理后带有换行）、第三方服务商
                         ************************************************************************/

                        #region 针对content内容中含有推理内容的优化

                        if (contentValue == "<think>")
                            isThink = true;
                        if (contentValue == "</think>")
                        {
                            isThink = false;
                            // 跳过当前内容
                            return;
                        }

                        if (isThink)
                            return;

                        #endregion

                        #region 针对推理过后带有换行的情况进行优化

                        // 优化推理模型思考结束后的\n\n符号
                        if (string.IsNullOrWhiteSpace(sb.ToString()) && string.IsNullOrWhiteSpace(contentValue))
                            return;

                        sb.Append(contentValue);

                        #endregion

                        onDataReceived?.Invoke(contentValue);
                    }
                    catch
                    {
                        // Ignore
                        // * 适配OpenRouter等第三方服务流数据中包含与OpenAI官方API中不同的数据
                        // * 如 ": OPENROUTER PROCESSING"
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

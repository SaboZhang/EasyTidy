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
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

namespace EasyTidy.Service.AIService;

public partial class HunyuanService : ObservableObject, IAIServiceLlm
{
    public HunyuanService() : this(Guid.NewGuid(), "https://hunyuan.tencentcloudapi.com", "Hunyuan") { }

    public HunyuanService(
        Guid identify,
        string url,
        string name = "",
        ServiceType type = ServiceType.Azure,
        string appID = "", string appKey = "",
        bool isEnabled = true,
        string model = "hunyuan-turbos-latest")
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
    private string _model = string.Empty;
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
        if (string.IsNullOrEmpty(Url) /* || string.IsNullOrEmpty(AppKey)*/)
            throw new Exception("请先完善配置");

        if (request is not RequestModel req)
            throw new Exception($"请求数据出错: {request}");

        var source = req.Text;
        var language = req.Language;

        UriBuilder uriBuilder = new(Url);

        // 选择模型
        var a_model = Model.Trim();
        a_model = string.IsNullOrEmpty(a_model) ? "hunyuan-turbos-latest" : a_model;

        var path = "/v1/chat/completions";
        if (!(uriBuilder.Path == "/v1/chat/completions"))
            uriBuilder.Path = path;

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
            Model = a_model,
            Messages = a_messages,
            Temperature = a_temperature,
            Stream = true
        };

        var header = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {AppKey}" },
            { "Content-Type", "application/json" }
        };

        var jsonData = Json.SerializeForModel(reqData, PropertyCase.PascalCase);

        try
        {
            if (string.IsNullOrEmpty(AppID))
            {
                await HttpUtil.PostAsync(
                    uriBuilder.Uri,
                    header,
                    jsonData,
                    msg => ProcessResponseMessage(msg, onDataReceived),
                    token
                ).ConfigureAwait(false);
            }
            else
            {
                var headers = BuildAuthorization(
                    AppID,
                    AppKey,
                    "hunyuan",
                    uriBuilder.Host,
                    "ChatCompletions",
                    "2023-09-01",
                    jsonData.ToString(),
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    out var requestHeaders);

                var newUri = new UriBuilder(uriBuilder.Host) { Scheme = "https", Port = 443 };

                await HttpUtil.PostAsync(
                    newUri.Uri,
                    requestHeaders,
                    jsonData,
                    msg => ProcessResponseMessage(msg, onDataReceived),
                    token
                ).ConfigureAwait(false);
            }
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

    private static void ProcessResponseMessage(string msg, Action<string> onDataReceived)
    {
        if (string.IsNullOrEmpty(msg?.Trim())) return;

        var preprocessString = msg.Replace("data:", "").Trim();
        var parsedData = JsonConvert.DeserializeObject<JObject>(preprocessString);

        var finishReason = parsedData["Choices"]?.FirstOrDefault()?["FinishReason"]?.ToString();
        if (finishReason?.Equals("stop", StringComparison.OrdinalIgnoreCase) == true) return;

        var contentValue = parsedData["Choices"]?.FirstOrDefault()?["Delta"]?["Content"]?.ToString();
        if (!string.IsNullOrEmpty(contentValue))
            onDataReceived?.Invoke(contentValue);
    }

    private static string BuildAuthorization(
        string secretId,
        string secretKey,
        string service,
        string host,
        string action,
        string version,
        string payload,
        string timestamp,
        out Dictionary<string, string> headers)
    {
        const string algorithm = "TC3-HMAC-SHA256";
        var date = DateTimeOffset.FromUnixTimeSeconds(long.Parse(timestamp)).UtcDateTime.ToString("yyyy-MM-dd");

        // ********** Step 1: Canonical Request **********
        string httpRequestMethod = "POST";
        string canonicalUri = "/";
        string canonicalQueryString = "";
        string canonicalHeaders = $"content-type:application/json; charset=utf-8\nhost:{host}\nx-tc-action:{action}\n";
        string signedHeaders = "content-type;host;x-tc-action";
        string hashedRequestPayload = Sha256Hex(payload);
        string canonicalRequest = $"{httpRequestMethod}\n{canonicalUri}\n{canonicalQueryString}\n{canonicalHeaders}\n{signedHeaders}\n{hashedRequestPayload}";

        // ********** Step 2: String to Sign **********
        string credentialScope = $"{date}/{service}/tc3_request";
        string hashedCanonicalRequest = Sha256Hex(canonicalRequest);
        string stringToSign = $"{algorithm}\n{timestamp}\n{credentialScope}\n{hashedCanonicalRequest}";

        // ********** Step 3: Signature **********
        byte[] secretDate = HmacSha256(Encoding.UTF8.GetBytes("TC3" + secretKey), date);
        byte[] secretService = HmacSha256(secretDate, service);
        byte[] secretSigning = HmacSha256(secretService, "tc3_request");
        byte[] signatureBytes = HmacSha256(secretSigning, stringToSign);
        string signature = BitConverter.ToString(signatureBytes).Replace("-", "").ToLower();

        // ********** Step 4: Authorization **********
        string authorization = $"{algorithm} Credential={secretId}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";

        // 返回请求头
        headers = new Dictionary<string, string>
        {
            { "Authorization", authorization },
            { "Content-Type", "application/json; charset=utf-8" },
            { "Host", host },
            { "X-TC-Action", action },
            { "X-TC-Timestamp", timestamp },
            { "X-TC-Version", version }
        };

        return authorization;
    }

    private static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string Sha256Hex(string input)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

}

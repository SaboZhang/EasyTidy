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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using UglyToad.PdfPig.Tokens;

namespace EasyTidy.Service.AIService;

public partial class VolcengineService : ObservableObject, IAIServiceLlm
{
    private static readonly string _authUrl = "https://open.volcengineapi.com";
    public VolcengineService() : this(Guid.NewGuid(), "https://ark.cn-beijing.volces.com", "Volcengine") { }

    public VolcengineService(
        Guid identify,
        string url,
        string name = "",
        ServiceType type = ServiceType.Volcengine,
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
        a_model = string.IsNullOrEmpty(a_model) ? "doubao-1.5-pro-32k-250115" : a_model;

        var path = "/api/v3/chat/completions";
        if (uriBuilder.Path != path)
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
            model = a_model,
            messages = a_messages,
            temperature = a_temperature,
            stream = true
        };

        var jsonData = Json.SerializeForModel(reqData, PropertyCase.CamelCase);

        if (!string.IsNullOrEmpty(AppID))
        {
            AppKey = await GetTempApiKeyAsync(a_model);
        }

        var header = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {AppKey}" },
            { "Content-Type", "application/json" }
        };

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

    private async Task<string> GetTempApiKeyAsync(string a_model)
    {
        UriBuilder authBuilder = new(_authUrl);
        authBuilder.Query = "Action=GetApiKey&Version=2024-01-01";
        var authData = new
        {
            DurationSeconds = 86400,
            ResourceType = "endpoint",
            ResourceIds = new string[] { a_model }
        };
        var authJsonData = Json.SerializeForModel(authData, PropertyCase.PascalCase);
        var header = BuildSignedHeaders(authBuilder.Uri, authJsonData, "ark", "cn-beijing");
        var authResutl = await HttpUtil.PostAsync(authBuilder.Uri, header, authJsonData, CancellationToken.None).ConfigureAwait(false);
        var apiKeyJson = JsonConvert.DeserializeObject<JObject>(authResutl) ?? throw new Exception("获取API Key失败");
        var apiKey = apiKeyJson["Result"]?["ApiKey"]?.Value<string>() ?? throw new Exception("获取API Key失败");
        return apiKey;
    }

    public Dictionary<string, string> BuildSignedHeaders(Uri url, string jsonPayload, string service, string region)
    {
        // 1. 准备请求时间戳（UTC 时间，格式为 yyyyMMddTHHmmssZ）
        string requestDate = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
        string shortDate = requestDate.Substring(0, 8);

        // 2. 计算请求体的 SHA256 摘要（十六进制）
        string payloadHash = HashSha256(jsonPayload);

        string host = url.Host;
        string canonicalUri = url.AbsolutePath;
        string canonicalQueryString = url.Query.TrimStart('?');

        // 4. 设置 Content-Type
        string contentType = "application/json; charset=utf-8";

        // 5. 构造 Canonical Headers（按字母排序）
        var canonicalHeaders = new StringBuilder();
        canonicalHeaders.Append("content-type:" + contentType + "\n");
        canonicalHeaders.Append("host:" + host + "\n");
        canonicalHeaders.Append("x-content-sha256:" + payloadHash + "\n");
        canonicalHeaders.Append("x-date:" + requestDate + "\n");

        // 6. 签名使用的头列表
        string signedHeaders = "content-type;host;x-content-sha256;x-date";

        // 7. 构造规范请求（CanonicalRequest）
        string httpMethod = "POST";
        string canonicalRequest = $"{httpMethod}\n{canonicalUri}\n{canonicalQueryString}\n" +
                                  canonicalHeaders.ToString() + "\n" +
                                  signedHeaders + "\n" +
                                  payloadHash;

        // 8. 计算规范请求的 SHA256 哈希
        string canonicalRequestHash = HashSha256(canonicalRequest);

        // 9. 构造待签名字符串（StringToSign）
        string algorithm = "HMAC-SHA256";
        string credentialScope = $"{shortDate}/{region}/{service}/request";
        string stringToSign = $"{algorithm}\n{requestDate}\n{credentialScope}\n{canonicalRequestHash}";

        // 10. 计算签名
        byte[] signingKey = GetSignatureKey(AppKey, shortDate, region, service);
        string signature = HmacSha256(signingKey, stringToSign);

        // 11. 构造 Authorization 头
        string credential = $"{AppID}/{credentialScope}";
        string authorizationHeader = $"{algorithm} Credential={credential}, SignedHeaders={signedHeaders}, Signature={signature}";

        // 12. 返回包含签名的头部字典
        var headers = new Dictionary<string, string>
        {
            { "Content-Type", contentType },
            { "Host", host },
            { "X-Date", requestDate },
            { "X-Content-Sha256", payloadHash },
            { "Authorization", authorizationHeader }
        };

        return headers;
    }


    public static string HashSha256(string data)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            byte[] hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    public static string HmacSha256(byte[] key, string data)
    {
        using (var hmac = new HMACSHA256(key))
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            byte[] hash = hmac.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    public static byte[] GetSignatureKey(string secretKey, string date, string region, string service)
    {
        byte[] kDate = HmacSha256Bytes(Encoding.UTF8.GetBytes(secretKey), date);
        byte[] kRegion = HmacSha256Bytes(kDate, region);
        byte[] kService = HmacSha256Bytes(kRegion, service);
        byte[] kSigning = HmacSha256Bytes(kService, "request");
        return kSigning;
    }

    private static byte[] HmacSha256Bytes(byte[] key, string data)
    {
        using (var hmac = new HMACSHA256(key))
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            return hmac.ComputeHash(bytes);
        }
    }

}

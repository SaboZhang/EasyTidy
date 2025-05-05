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

namespace EasyTidy.Service.AIService;

public partial class VolcengineService : ObservableObject, IAIServiceLlm
{
    public VolcengineService() : this(Guid.NewGuid(), "https://ark.cn-beijing.volces.com", "Volcengine") { }

    public VolcengineService(
        Guid identify,
        string url,
        string name = "",
        ServiceType type = ServiceType.Volcengine,
        string appID = "", string appKey = "",
        bool isEnabled = true,
        string model = "doubao-1.5-pro-32k-250115"
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

        var header = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {AppKey}" },
            { "Content-Type", "application/json" }
        };
        var jsonData = Json.SerializeForModel(reqData, PropertyCase.CamelCase);

        if (!string.IsNullOrEmpty(AppID))
        {
            header = BuildSignedHeadersAsync(uriBuilder.Uri.ToString(), jsonData.ToString() ,"ark");
        }

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

    /// <summary>
    /// 发送带签名的 POST 请求（application/x-www-form-urlencoded）。
    /// </summary>
    public Dictionary<string, string> BuildSignedHeadersAsync(string url, string parameters, string service)
    {
        // 1. 准备请求时间戳（UTC 时间，格式为 yyyyMMddTHHmmssZ），并设置 X-Date 头
        string requestDate = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
        // 2. 准备请求体：URL 编码形式
        //var content = new FormUrlEncodedContent(parameters);
        //string payload = await content.ReadAsStringAsync();
        // 3. 计算请求体的 SHA256 摘要（十六进制），并设置 X-Content-SHA256 头
        string payloadHash = SignatureHelper.HashSha256(parameters);
        // 4. 准备请求头，包括 Host、Content-Type、X-Date、X-Content-SHA256
        var uri = new Uri(url);
        string host = uri.Host;
        // 注意：Content-Type 固定为 application/x-www-form-urlencoded; charset=utf-8
        string contentType = "application/json; charset=utf-8";

        // 构造 Canonical Headers（按字母排序）：content-type、host、x-content-sha256、x-date
        // 拼接格式：key:value\n
        var canonicalHeaders = new StringBuilder();
        canonicalHeaders.Append("content-type:" + contentType + "\n");
        canonicalHeaders.Append("host:" + host + "\n");
        canonicalHeaders.Append("x-content-sha256:" + payloadHash + "\n");
        canonicalHeaders.Append("x-date:" + requestDate + "\n");

        // 签名使用的头列表（按分号分隔，均小写）
        string signedHeaders = "content-type;host;x-content-sha256;x-date";

        // 5. 构造规范请求（CanonicalRequest）
        //    格式：HTTPMethod + '\n' + CanonicalURI + '\n' + CanonicalQueryString + '\n'
        //         + CanonicalHeaders + '\n' + SignedHeaders + '\n' + HexEncode(Hash(RequestPayload))
        string httpMethod = "POST";
        string canonicalUri = uri.AbsolutePath;  // 请求路径
        string canonicalQueryString = uri.Query.TrimStart('?');  // 查询参数，这里假设无额外查询参数
        string canonicalRequest = $"{httpMethod}\n{canonicalUri}\n{canonicalQueryString}\n" +
                                  canonicalHeaders.ToString() + "\n" +
                                  signedHeaders + "\n" +
                                  payloadHash;  // payloadHash 已是十六进制字符串
                                                // 规范请求哈希
        string canonicalRequestHash = SignatureHelper.HashSha256(canonicalRequest);

        // 6. 构造待签名字符串（StringToSign）
        //    格式：Algorithm + '\n' + RequestDate + '\n' + CredentialScope + '\n' + HexEncode(Hash(CanonicalRequest))
        string algorithm = "HMAC-SHA256";
        string dateScope = DateTime.UtcNow.ToString("yyyyMMdd");
        string credentialScope = $"{dateScope}/cn-beijing/{service}/request";
        string stringToSign = $"{algorithm}\n{requestDate}\n{credentialScope}\n{canonicalRequestHash}";

        // 7. 计算签名：先派生签名密钥，再对 StringToSign 计算 HMAC-SHA256
        byte[] signingKey = SignatureHelper.GetSignatureKey(AppKey, dateScope, "cn-beijing", service);
        string signature = SignatureHelper.ComputeSignature(stringToSign, signingKey);

        // 8. 构造 Authorization 头
        //    格式：HMAC-SHA256 Credential={AccessKeyId}/{CredentialScope}, SignedHeaders={SignedHeaders}, Signature={Signature}
        string credential = $"{AppID}/{credentialScope}";
        string authorizationHeader = $"{algorithm} Credential={credential}, SignedHeaders={signedHeaders}, Signature={signature}";

        // 9. 调用用户提供的 PostAsync 方法发送请求（包含表单内容和签名头）
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

    /// <summary>
    /// 签名辅助工具类：计算 SHA256 哈希、HMAC-SHA256 签名、派生签名密钥等。
    /// </summary>
    public static class SignatureHelper
    {
        /// <summary>
        /// 计算输入字符串的 SHA256 哈希，并返回小写十六进制字符串。
        /// </summary>
        public static string HashSha256(string data)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                byte[] hash = sha256.ComputeHash(bytes);
                // 转换为十六进制字符串
                StringBuilder sb = new StringBuilder();
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>
        /// 使用指定的密钥对字符串计算 HMAC-SHA256 签名，返回小写十六进制字符串。
        /// </summary>
        public static string HmacSha256(byte[] key, string data)
        {
            using (var hmac = new HMACSHA256(key))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                byte[] hash = hmac.ComputeHash(bytes);
                // 转换为十六进制字符串
                StringBuilder sb = new StringBuilder();
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>
        /// 派生签名密钥：根据 SecretKey、日期、区域、服务名生成 HMAC-SHA256 签名密钥。
        /// </summary>
        public static byte[] GetSignatureKey(string secretKey, string date, string region, string service)
        {
            byte[] kSecret = Encoding.UTF8.GetBytes("HMACSHA256" + secretKey);
            byte[] kDate = new HMACSHA256(kSecret).ComputeHash(Encoding.UTF8.GetBytes(date));
            byte[] kRegion = new HMACSHA256(kDate).ComputeHash(Encoding.UTF8.GetBytes(region));
            byte[] kService = new HMACSHA256(kRegion).ComputeHash(Encoding.UTF8.GetBytes(service));
            byte[] kSigning = new HMACSHA256(kService).ComputeHash(Encoding.UTF8.GetBytes("request"));
            return kSigning;
        }

        /// <summary>
        /// 使用签名密钥计算最终签名值。
        /// </summary>
        public static string ComputeSignature(string stringToSign, byte[] signingKey)
        {
            return HmacSha256(signingKey, stringToSign);
        }

    }
}

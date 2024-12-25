using EasyTidy.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebDAVClient;
using WebDAVClient.Model;

namespace EasyTidy.Util;

public class WebDavClient
{
    private readonly IClient _client;

    public WebDavClient(string webDavUrl, string username = null, string password = "")
    {
        (string serverUrl, string basePath) = ExtractServerUrlAndBasePath(webDavUrl);
        _client = new Client(username is null ? null : new NetworkCredential { UserName = username, Password = password });
        _client.Server = serverUrl;
        _client.BasePath = basePath;
    }

    private static (string serverUrl, string basePath) ExtractServerUrlAndBasePath(string inputUrl)
    {
        if (string.IsNullOrEmpty(inputUrl))
        {
            throw new ArgumentException("Input URL cannot be null or empty.", nameof(inputUrl));
        }

        try
        {
            // 对输入URL进行非ASCII字符转义
            inputUrl = EncodeWebDavUrl(inputUrl);

            var uri = new Uri(inputUrl);

            // 优化服务器地址提取，考虑非ASCII字符
            var serverUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}";

            // 优化basePath的提取
            var basePath = uri.AbsolutePath;

            // 如果basePath为空或仅为斜杠（""或"/"），则设为"/"
            basePath = string.IsNullOrEmpty(basePath) || basePath == "/" ? "/" : basePath;

            return (serverUrl, basePath);
        }
        catch (UriFormatException ex)
        {
            throw new ArgumentException("Invalid URL format.", nameof(inputUrl), ex);
        }
        catch (ArgumentException ex)
        {
            // 捕获并处理因字符串为空或无效而可能抛出的 ArgumentException
            throw new ArgumentException("Invalid URL input.", nameof(inputUrl), ex);
        }
    }


    public async Task<IEnumerable<WebDavItem>> ListItemsAsync(string path = "")
    {
        var itemsFromClient = await _client.List(path);
        return itemsFromClient.Select(item => new WebDavItem
        {
            Href = item.Href,
            IsCollection = item.IsCollection,
            LastModified = item.LastModified,
            ContentLength = item.ContentLength,
            ContentType = item.ContentType,
            DisplayName = item.DisplayName
        });
    }

    public async Task<List<WebDavItem>> RecursivelyListFilesAsync(string webDavPath = "")
    {
        var result = new List<WebDavItem>();

        // 获取当前路径下的文件和目录列表
        var entries = await _client.List(webDavPath);

        foreach (var entry in entries)
        {
            var item = new WebDavItem
            {
                Href = entry.Href,
                IsCollection = entry.IsCollection,
                LastModified = entry.LastModified,
                ContentLength = entry.ContentLength,
                ContentType = entry.ContentType,
                DisplayName = entry.DisplayName
            };

            if (entry.IsCollection)
            {
                // 递归进入子目录
                var subItems = await RecursivelyListFilesAsync(item.Href);
                result.AddRange(subItems);
            }
            else
            {
                // 添加文件到结果集
                result.Add(item);
            }
        }

        return result;
    }

    public async Task<Item> GetFolderAsync(string folderHref)
    {
        var folderReloaded = await _client.GetFolder(folderHref);
        return folderReloaded;
    }

    public async Task<WebDavItem> DownloadItemAsync(WebDavItem item, string localFilePath)
    {
        var directory = Path.GetDirectoryName(localFilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempFileName = Path.GetTempFileName();
        using (var tempFile = File.OpenWrite(tempFileName))
        using (var stream = await _client.Download(item.Href))
        {
            await stream.CopyToAsync(tempFile);
        }

        File.Move(tempFileName, localFilePath, true);
        return item;
    }

    public async Task<bool> UploadFileAsync(string folderHref, string localFilePath, string remoteFileName = null)
    {
        var remoteName = remoteFileName ?? Path.GetFileName(localFilePath);
        using (var fileStream = File.OpenRead(localFilePath))
        {
            var fileUploaded = await _client.Upload(folderHref, fileStream, remoteName);
            return fileUploaded;
        }
    }

    public async Task<bool> CreateFolderAsync(string parentFolderPath, string folderName)
    {
        var tempFolderName = folderName;
        var isFolderCreated = await _client.CreateDir(parentFolderPath, tempFolderName);
        return isFolderCreated;
    }

    public async Task<bool> DeleteFolderAsync(string folderHref)
    {
        var folderToDelete = await _client.GetFolder(folderHref);
        await _client.DeleteFolder(folderToDelete.Href);
        return true; // Assuming delete operation was successful
    }

    private static string EncodeWebDavUrl(string url)
    {
        // 预检查URL格式
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            throw new ArgumentException("Invalid URL format.", nameof(url));
        }

        Uri parsedUri = new Uri(url);
        string schemeAndAuthority = parsedUri.GetLeftPart(UriPartial.Authority);
        string pathAndQuery = parsedUri.PathAndQuery;

        string[] pathAndQueryParts = pathAndQuery.Split('/');
        string[] encodedParts = Array.ConvertAll(pathAndQueryParts, EncodePossibleNonAsciiCharactersWithoutReencoding);

        string result = schemeAndAuthority + string.Join("/", encodedParts);

        // 添加末尾斜杠，但需确保它不会出现在已编码的最后一个路径部分
        if (!result.EndsWith("/") && !encodedParts.Last().Contains("%2F"))
        {
            result += "/";
        }

        return result;
    }

    private static string EncodePossibleNonAsciiCharactersWithoutReencoding(string part)
    {
        // 检查是否有已编码的百分号（%），如果有则说明该部分可能已编码过
        if (part.Contains('%'))
        {
            // 使用正则表达式匹配尚未编码的非ASCII字符（不包含已编码的部分）
            MatchCollection nonEncodedNonAsciiMatches = Regex.Matches(part, @"(?<!%)%[0-9A-Fa-f]{2}|[^%]+");

            StringBuilder sb = new StringBuilder();
            foreach (Match match in nonEncodedNonAsciiMatches)
            {
                string matchedPart = match.Value;
                if (matchedPart.StartsWith("%")) // 已编码部分，直接保留
                {
                    sb.Append(matchedPart);
                }
                else // 未编码部分，进行编码
                {
                    sb.Append(Uri.EscapeDataString(matchedPart));
                }
            }

            return sb.ToString();
        }
        else // 该部分没有已编码字符，整体进行编码
        {
            return Uri.EscapeDataString(part);
        }
    }
}

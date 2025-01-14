using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EasyTidy.Util;

public class CryptoUtil
{
    // 默认密钥和初始化向量
    private const string DEFAULTKEY = "!H0O<[W}";

    private const string DEFAULTIV = ".l;x$0BK";

    /// <summary>
    ///     使用DES算法进行加密
    /// </summary>
    /// <param name="data">要加密的数据</param>
    /// <param name="key">8位字符的密钥字符串</param>
    /// <param name="iv">8位字符的初始化向量字符串</param>
    /// <returns>加密后的Base64字符串</returns>
    public static string DesEncrypt(string data, string? key = null, string? iv = null)
    {
        // 将输入数据转换为ASCII编码的字节数组
        var bytes = Encoding.ASCII.GetBytes(data);

        // 将密钥和初始化向量转换为ASCII编码的字节数组，如果未提供则使用默认值
        var byKey = Encoding.ASCII.GetBytes(key ?? DEFAULTKEY);
        var byIV = Encoding.ASCII.GetBytes(iv ?? DEFAULTIV);

        // 创建DES加密算法实例
        using var des = DES.Create();
        // 创建内存流
        using var ms = new MemoryStream();
        // 创建加密流
        using var cs = new CryptoStream(ms, des.CreateEncryptor(byKey, byIV), CryptoStreamMode.Write);
        // 写入加密后的数据到内存流
        cs.Write(bytes, 0, bytes.Length);
        // 刷新加密流的最终块
        cs.FlushFinalBlock();
        // 将内存流中的数据转换为Base64字符串并返回
        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    ///     使用DES算法进行解密
    /// </summary>
    /// <param name="data">要解密的Base64字符串</param>
    /// <param name="key">8位字符的密钥字符串（必须与加密时相同）</param>
    /// <param name="iv">8位字符的初始化向量字符串（必须与加密时相同）</param>
    /// <returns>解密后的字符串</returns>
    public static string DesDecrypt(string data, string? key = null, string? iv = null)
    {
        try
        {
            // 将Base64字符串转换为字节数组
            var bytes = Convert.FromBase64String(data);

            // 将密钥和初始化向量转换为ASCII编码的字节数组，如果未提供则使用默认值
            var byKey = Encoding.ASCII.GetBytes(key ?? DEFAULTKEY);
            var byIV = Encoding.ASCII.GetBytes(iv ?? DEFAULTIV);

            // 创建DES解密算法实例
            using var des = DES.Create();
            // 创建内存流
            using var ms = new MemoryStream();
            // 创建解密流
            using var cs = new CryptoStream(ms, des.CreateDecryptor(byKey, byIV), CryptoStreamMode.Write);
            // 写入解密后的数据到内存流
            cs.Write(bytes, 0, bytes.Length);
            // 刷新解密流的最终块
            cs.FlushFinalBlock();
            // 将内存流中的数据转换为ASCII编码的字符串并返回
            return Encoding.ASCII.GetString(ms.ToArray());
        }
        catch (Exception)
        {
            // 如果出现问题则返回传入值
            return data;
        }
    }

    /// <summary>
    /// 文件加密,支持使用常用解密工具进行解密 AES-256 with PBKDF2-derived key
    /// </summary>
    /// <param name="inputFile"></param>
    /// <param name="outputFile"></param>
    /// <param name="password"></param>
    public static void EncryptFile(string inputFile, string outputFile, string password)
    {
        if (string.IsNullOrWhiteSpace(inputFile) || !File.Exists(inputFile))
        {
            throw new ArgumentException("输入文件路径无效或文件不存在。");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("密码不能为空。");
        }

        try
        {
            using (var inputFileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (var outputFileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // 生成随机盐值
                byte[] salt = RandomNumberGenerator.GetBytes(16);

                // 使用推荐的构造方法，指定 HMACSHA256 和安全的迭代次数
                using (var keyDerivationFunction = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
                {
                    aes.Key = keyDerivationFunction.GetBytes(32);
                    aes.IV = RandomNumberGenerator.GetBytes(16);
                }

                // 写入文件头：盐值和 IV
                outputFileStream.Write(salt, 0, salt.Length);
                outputFileStream.Write(aes.IV, 0, aes.IV.Length);

                // 使用分块加密方式
                byte[] buffer = new byte[1048576]; // 1 MB 缓冲区
                int bytesRead;

                using (var cryptoStream = new CryptoStream(outputFileStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    while ((bytesRead = inputFileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        cryptoStream.Write(buffer, 0, bytesRead);
                    }
                }
            }

            // 为加密文件添加后缀
            string encryptedFileName = outputFile + "enc";
            if (File.Exists(encryptedFileName))
            {
                File.Delete(encryptedFileName); // 删除已存在的加密文件以避免冲突
            }
            File.Move(outputFile, encryptedFileName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("加密过程中发生错误。", ex);
        }
    }

    /// <summary>
    /// 文件解密
    /// </summary>
    /// <param name="inputFile"></param>
    /// <param name="outputFile"></param>
    /// <param name="password"></param>
    public static void DecryptFile(string inputFile, string outputFile, string password)
    {
        if (string.IsNullOrWhiteSpace(inputFile) || !File.Exists(inputFile))
        {
            throw new ArgumentException("输入文件路径无效或文件不存在。");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("密码不能为空。");
        }

        try
        {
            using (var inputFileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (var outputFileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // 从文件头读取盐值和 IV
                byte[] salt = new byte[16];
                inputFileStream.Read(salt, 0, salt.Length);

                byte[] iv = new byte[16];
                inputFileStream.Read(iv, 0, iv.Length);

                // 派生密钥
                using (var keyDerivationFunction = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
                {
                    aes.Key = keyDerivationFunction.GetBytes(32);
                    aes.IV = iv;
                }

                // 使用分块解密方式
                byte[] buffer = new byte[1048576]; // 1 MB 缓冲区
                int bytesRead;

                using (var cryptoStream = new CryptoStream(inputFileStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    while ((bytesRead = cryptoStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        outputFileStream.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("解密过程中发生错误。", ex);
        }
    }

}

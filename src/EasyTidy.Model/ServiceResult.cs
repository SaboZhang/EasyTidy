using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Model;

public class ServiceResult
{
    public bool IsSuccess { get; set; } = true;

    public object? Result { get; set; }

    public Exception? Exception { get; } // 可选，如果你想保留异常的详细信息

    /// <summary>
    /// 成功时的构造函数
    /// </summary>
    /// <param name="result"></param>
    private ServiceResult(object result)
    {
        IsSuccess = true;
        Result = result;
        Exception = null;
    }

    /// <summary>
    /// 失败时的构造函数
    /// </summary>
    private ServiceResult(string errorMessage, Exception? exception = null)
    {
        IsSuccess = false;
        Result = null;
        Exception = null;
    }

    private ServiceResult()
    {
        IsSuccess = true;
        Result = string.Empty;
        Exception = null;
    }

    /// <summary>
    ///     静态方法用于清空
    /// </summary>
    /// <returns></returns>
    public static ServiceResult Reset => new();

    /// <summary>
    ///     静态方法用于创建成功的结果
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public static ServiceResult Success(object result)
    {
        return new ServiceResult(result);
    }

    /// <summary>
    ///     静态方法用于创建失败的结果
    /// </summary>
    /// <param name="errorMessage"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    public static ServiceResult Fail(string errorMessage, Exception? exception = null)
    {
        return new ServiceResult(errorMessage, exception);
    }
}

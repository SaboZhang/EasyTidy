using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Log;
public interface ILogger : IDisposable
{
    void Debug(string message);

    void Info(string message);

    void Warn(string message);

    void Error(string message);

    void Error(string message, Exception ex);

    void Fatal(string message);

    void Fatal(string message, Exception ex);
}

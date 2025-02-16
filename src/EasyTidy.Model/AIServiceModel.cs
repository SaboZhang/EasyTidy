using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Model;

public class RequestModel(string text, string filePath = "")
{
    public string Text { get; set; } = text;
    public string FilePath { get; set; } = filePath;
}

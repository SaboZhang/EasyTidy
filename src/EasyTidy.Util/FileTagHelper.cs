using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace EasyTidy.Util;

public class FileTagHelper
{
    public static async void SetKeywords(string filePath, string keywords)
    {
        await Task.Run(() =>
        {
            var file = TagLib.File.Create(filePath);
            file.Tag.Comment = keywords;
            file.Save();
        });
    }
}

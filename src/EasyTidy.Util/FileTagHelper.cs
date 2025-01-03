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

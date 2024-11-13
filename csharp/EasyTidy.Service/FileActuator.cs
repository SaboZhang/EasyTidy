using EasyTidy.Log;
using EasyTidy.Model;
using System;
using System.IO;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Service;

public class FileActuator
{
    public static void OnExecuteMoveFile(string source, string target, FileOperationType fileOperationType)
    {
        try
        {
            string[] files = Directory.GetFiles(source);
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string destinationPath = Path.Combine(target, fileName);
                string newFileName;
                FileInfo sourceFileInfo = new(file);

                if (File.Exists(destinationPath))
                {
                    switch (fileOperationType)
                    {
                        case FileOperationType.Skip:
                            continue;
                        case FileOperationType.Override:
                        case FileOperationType.OverrideIfSizesDiffer:
                        case FileOperationType.OverwriteIfNewer:
                            if (fileOperationType == FileOperationType.Override)
                            {
                                File.Move(file, destinationPath);
                            }
                            else if (fileOperationType == FileOperationType.OverwriteIfNewer)
                            {
                                FileInfo destinationFileInfo = new(destinationPath);
                                if (sourceFileInfo.LastWriteTime > destinationFileInfo.LastWriteTime)
                                {
                                    File.Move(file, destinationPath);
                                }
                            }
                            else if (fileOperationType == FileOperationType.OverrideIfSizesDiffer)
                            {
                                FileInfo destinationFileInfo = new(destinationPath);
                                if (sourceFileInfo.Length != destinationFileInfo.Length)
                                {
                                    File.Move(file, destinationPath);
                                }
                            }
                            break;
                        case FileOperationType.ReNameAppend:
                            int count = 1;
                            newFileName = Path.GetFileNameWithoutExtension(fileName) + "(" + count.ToString() + ")" + Path.GetExtension(fileName);
                            while (File.Exists(Path.Combine(target, newFileName)))
                            {
                                count++;
                                newFileName = Path.GetFileNameWithoutExtension(fileName) + "(" + count.ToString() + ")" + Path.GetExtension(fileName);
                            }
                            destinationPath = Path.Combine(target, newFileName);
                            File.Move(file, destinationPath);
                            break;
                        case FileOperationType.ReNameAddDate:
                            string datePart = DateTime.Now.ToString("yyyyMMddHHmmss");
                            newFileName = Path.GetFileNameWithoutExtension(fileName) + "-" + datePart + Path.GetExtension(fileName);
                            destinationPath = Path.Combine(target, newFileName);
                            File.Move(file, destinationPath);
                            break;
                    }
                }
                else
                {
                    File.Move(file, destinationPath);
                }
            }
        }
        catch (Exception ex)
        {
            LogService.Logger.Error("文件移动失败", ex);
        }
    }

    public static void OnExecuteCopyFile(string source, string target, FileOperationType fileOperationType)
    {
        try
        {

        }
        catch (Exception ex)
        {
            LogService.Logger.Error("文件复制失败", ex);
        }
    }

    public static void OnExecuteDeleteFile(string source, string target, FileOperationType fileOperationType)
    {

    }

    public static void OnExecuteRenameFile(string source, string target, FileOperationType fileOperationType)
    {

    }
}

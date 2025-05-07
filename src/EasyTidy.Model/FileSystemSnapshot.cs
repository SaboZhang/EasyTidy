namespace EasyTidy.Model;

public class FileSystemSnapshot
{
    public FileSystemNode RootNode { get; set; }
    public int TotalFolders { get; set; }
    public int TotalFiles { get; set; }
}

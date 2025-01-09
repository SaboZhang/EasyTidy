using System;
using System.Collections.Generic;

namespace EasyTidy.Model;

public class FileSystemNode
{
    public string Name { get; set; } // 名称
    public string Path { get; set; } // 完整路径
    public bool IsFolder { get; set; } // 是否为文件夹
    public long? Size { get; set; } // 文件大小（如果是文件）
    public DateTime? ModifiedDate { get; set; } // 修改日期
    public int FolderCount { get; set; } // 子目录数量
    public int FileCount { get; set; } // 子文件数量
    public List<FileSystemNode> Children { get; set; } = new(); // 子节点列表
}

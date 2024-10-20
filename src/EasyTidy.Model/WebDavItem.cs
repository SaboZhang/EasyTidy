using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Model;

public class WebDavItem
{
    // 基本属性
    public string Href { get; set; } // 文件或目录的完整URL路径
    public bool IsCollection { get; set; } // 是否为目录（文件夹）
    public DateTime? LastModified { get; set; } // 最后修改时间
    public long? ContentLength { get; set; } // 文件大小（仅对文件有效）
    public string ContentType { get; set; } // MIME类型（仅对文件有效）
    public string DisplayName { get; set; } // 文件/文件夹名称
}

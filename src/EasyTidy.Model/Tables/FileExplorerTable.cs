using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyTidy.Model;

[Table("FileExplorer")]
public class FileExplorerTable
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID { get; set; }

    public string TaskName { get; set; }

    public string TaskRule { get; set; }

    public string TaskSource { get; set; }

    public bool Shortcut { get; set; }

    public string TaskTarget { get; set; }

    public OperationMode OperationMode { get; set; }

    public bool IsEnabled { get; set; }

}

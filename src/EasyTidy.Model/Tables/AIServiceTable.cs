using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Model;

public class AIServiceTable
{

    public int Id { get; set; }
    public string Name { get; set; }

    public Guid Identify { get; set; }

    public ServiceType Type { get; set; }

    public bool IsEnabled { get; set; }

    public string Url { get; set; }

    public string AppID { get; set; }

    public string AppKey { get; set; }

    public string Model { get; set; }

    public string? UserDefinePromptsJson { get; set; }
}

using System;

namespace EasyTidy.Model;

public class DefaultChatModel
{
    public string ModelName { get; set; } = string.Empty;
    public Guid Identifier { get; set; }
    public ServiceType ServiceType { get; set; }
    public string DisplayName { get; set; } = string.Empty;

}

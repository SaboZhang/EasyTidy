using System;
using ModelContextProtocol.Client;

namespace EasyTidy.Service;

public class McpServiceManager
{
    private readonly Dictionary<string, IMcpClient> _clients = new();

    
}

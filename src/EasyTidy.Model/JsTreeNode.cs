using System;
using System.Collections.Generic;

namespace EasyTidy.Model;

public class JsTreeNode
{
    public string id { get; set; }
    public string parent { get; set; }
    public string text { get; set; }
    public List<string> children { get; set; } // 用于存储子节点的ID列表
}

using System;

namespace EasyTidy.Model;

public class FilterItem
{
    public Func<string, bool> Predicate { get; set; } = _ => false;
    public bool IsExclude { get; set; } = false;
    
}

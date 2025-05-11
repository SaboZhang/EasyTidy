using EasyTidy.Model;
using System;

namespace EasyTidy.Util.Strategy;

public class CustomFilterStrategy
{
    private readonly FileFilterStrategy _fileFilterStrategy = new();
    private readonly FolderFilterStrategy _folderFilterStrategy = new();

    public FilterCollection GenerateFilters(string rule)
    {
        var filters = new FilterCollection();

        if (rule.Contains(';') || rule.Contains('|'))
        {
            string[] parts = rule.Split([';', '|'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                AddFilters(filters, part);
            }
        }
        else
        {
            // 如果规则不包含分隔符，则直接检查并添加过滤器
            AddFilters(filters, rule);
        }

        return filters;
    }

    private void AddFilters(FilterCollection filters, string part)
    {
        if (FilterUtil.IsFolderRule(part))
        {
            foreach (var filter in _folderFilterStrategy.GenerateFilters(part))
            {
                filters.Add(filter);
            }
        }
        else
        {
            foreach (var filter in _fileFilterStrategy.GenerateFilters(part))
            {
                filters.Add(filter);
            }
        }
    }

}

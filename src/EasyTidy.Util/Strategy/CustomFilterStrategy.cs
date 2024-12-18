using System;
using EasyTidy.Model;

namespace EasyTidy.Util.Strategy;

public class CustomFilterStrategy
{
    private readonly FileFilterStrategy _fileFilterStrategy;
    private readonly FolderFilterStrategy _folderFilterStrategy;

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

    private static bool IsFolderRule(string rule)
    {
        // 判断规则是否为文件夹规则，规则包含 "**" 或 "##" 时视为文件夹规则
        return FilterUtil.ContainsTwoConsecutiveChars(rule, '*') || FilterUtil.ContainsTwoConsecutiveChars(rule, '#');
    }

    private void AddFilters(FilterCollection filters, string part)
    {
        if (IsFolderRule(part))
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

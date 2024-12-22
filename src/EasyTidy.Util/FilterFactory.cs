using EasyTidy.Model;
using EasyTidy.Util.Strategy;
using EasyTidy.Util.UtilInterface;
using System.Collections.Generic;
using System.IO;

namespace EasyTidy.Util;

public static class FilterFactory
{
    public static List<IFileFilter> GetFilters(FilterTable filter)
    {
        var filters = new List<IFileFilter>();

        // 文件大小过滤器
        if (filter.IsSizeSelected)
        {
            filters.Add(new SizeFilter(filter.SizeValue, filter.SizeUnit, filter.SizeOperator));
        }

        // 创建日期过滤器
        if (filter.IsCreateDateSelected)
        {
            filters.Add(new DateFilter(filter.CreateDateValue, filter.CreateDateUnit, filter.CreateDateOperator, DateType.Create));
        }

        // 编辑日期过滤器
        if (filter.IsEditDateSelected)
        {
            filters.Add(new DateFilter(filter.EditDateValue, filter.EditDateUnit, filter.EditDateOperator, DateType.Edit));
        }

        // 访问日期过滤器
        if (filter.IsVisitDateSelected)
        {
            filters.Add(new DateFilter(filter.VisitDateValue, filter.VisitDateUnit, filter.VisitDateOperator, DateType.Visit));
        }

        // 文件属性过滤器
        if (filter.IsArchiveSelected)
            filters.Add(new AttributeFilter(FileAttributes.Archive, filter.ArchiveValue));

        if (filter.IsHiddenSelected)
            filters.Add(new AttributeFilter(FileAttributes.Hidden, filter.HiddenValue));

        if (filter.IsReadOnlySelected)
            filters.Add(new AttributeFilter(FileAttributes.ReadOnly, filter.ReadOnlyValue));

        if (filter.IsSystemSelected)
            filters.Add(new AttributeFilter(FileAttributes.System, filter.SystemValue));

        if (filter.IsTempSelected)
            filters.Add(new AttributeFilter(FileAttributes.Temporary, filter.TempValue));

        // 包含文件名过滤器
        if (filter.IsIncludeSelected)
            filters.Add(new IncludeFilter(filter.IncludedFiles));

        // 内容过滤器
        if (filter.IsContentSelected)
            filters.Add(new ContentFilter(filter.ContentValue, filter.ContentOperator));

        return filters;
    }
}

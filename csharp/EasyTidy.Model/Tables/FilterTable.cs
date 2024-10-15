using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Model;

[Table("Filters")]
public class FilterTable
{

    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string FilterName { get; set; }

    public bool IsSizeSelected { get; set; }
    public ComparisonResult SizeOperator { get; set; }
    public string? SizeValue { get; set; }
    public SizeUnit SizeUnit { get; set; }

    public bool IsCreateDateSelected { get; set; }
    public ComparisonResult CreateDateOperator { get; set; }
    public string? CreateDateValue { get; set; }
    public DateUnit CreateDateUnit { get; set; }

    public bool IsEditDateSelected { get; set; }
    public ComparisonResult EditDateOperator { get; set; }
    public string? EditDateValue { get; set; }
    public DateUnit EditDateUnit { get; set; }

    public bool IsVisitDateSelected { get; set; }
    public ComparisonResult VisitDateOperator { get; set; }
    public string? VisitDateValue { get; set; }
    public DateUnit VisitDateUnit { get; set; }

    public bool IsArchiveSelected { get; set; }
    public YesOrNo ArchiveValue { get; set; }

    public bool IsHiddenSelected { get; set; }
    public YesOrNo HiddenValue { get; set; }

    public bool IsReadOnlySelected { get; set; }
    public YesOrNo ReadOnlyValue { get; set; }

    public bool IsSystemSelected { get; set; }
    public YesOrNo SystemValue { get; set; }

    public bool IsTempSelected { get; set; }
    public YesOrNo TempValue { get; set; }

    public bool IsIncludeSelected { get; set; }
    public string IncludedFiles { get; set; }

    public bool IsContentSelected { get; set; }
    public ContentOperatorEnum ContentOperator { get; set; }
    public string ContentValue { get; set; }

    // Character 相关字段
    [NotMapped]
    public virtual string CharacterValue { get; set; }

    public string BuildCharacterValue()
    {
        StringBuilder sb = new();
        bool isFirst = true;

        if (IsSizeSelected)
        {
            AppendCondition(sb, ref isFirst, $"文件大小：{EnumHelper.GetDisplayName(SizeOperator)} {SizeValue} {EnumHelper.GetDisplayName(SizeUnit)}");
        }
        if (IsCreateDateSelected)
        {
            AppendCondition(sb, ref isFirst, $"创建时间：{EnumHelper.GetDisplayName(CreateDateOperator)} {CreateDateValue} {EnumHelper.GetDisplayName(CreateDateUnit)}");
        }
        if (IsEditDateSelected)
        {
            AppendCondition(sb, ref isFirst, $"修改时间：{EnumHelper.GetDisplayName(EditDateOperator)} {EditDateValue} {EnumHelper.GetDisplayName(EditDateUnit)}");
        }
        if (IsVisitDateSelected)
        {
            AppendCondition(sb, ref isFirst, $"访问时间：{EnumHelper.GetDisplayName(VisitDateOperator)} {VisitDateValue} {EnumHelper.GetDisplayName(VisitDateUnit)}");
        }

        return sb.ToString();
    }

    // Attribute 相关字段
    [NotMapped]
    public virtual string AttributeValue { get; set; }

    public string BuildAttributeValue()
    {
        StringBuilder sb = new();
        bool isFirst = true;

        if (IsArchiveSelected)
        {
            AppendCondition(sb, ref isFirst, $"存档 = {EnumHelper.GetDisplayName(ArchiveValue)}");
        }
        if (IsHiddenSelected)
        {
            AppendCondition(sb, ref isFirst, $"隐藏 = {EnumHelper.GetDisplayName(HiddenValue)}");
        }
        if (IsReadOnlySelected)
        {
            AppendCondition(sb, ref isFirst, $"只读 = {EnumHelper.GetDisplayName(ReadOnlyValue)}");
        }
        if (IsSystemSelected)
        {
            AppendCondition(sb, ref isFirst, $"系统 = {EnumHelper.GetDisplayName(SystemValue)}");
        }
        if (IsTempSelected)
        {
            AppendCondition(sb, ref isFirst, $"临时 = {EnumHelper.GetDisplayName(TempValue)}");
        }

        return sb.ToString();
    }

    // Other 相关字段
    [NotMapped]
    public virtual string OtherValue { get; set; }

    public string BuildOtherValue()
    {
        StringBuilder sb = new();
        bool isFirst = true;

        if (IsIncludeSelected)
        {
            AppendCondition(sb, ref isFirst, $"包含的文件 = {IncludedFiles}");
        }
        if (IsContentSelected)
        {
            AppendCondition(sb, ref isFirst, $"文件内容：{ContentOperator} = {ContentValue}");
        }

        return sb.ToString();
    }

    private static void AppendCondition(StringBuilder sb, ref bool isFirst, string condition)
    {
        if (!isFirst)
        {
            sb.Append(" | ");
        }
        isFirst = false;
        sb.Append(condition);
    }
}

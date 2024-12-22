using System.Collections.ObjectModel;

namespace EasyTidy.Common.Model;

public partial class TaskItem : ObservableRecipient
{
    private readonly TaskItem? _parentItem;

    [ObservableProperty]
    public string _name;

    [ObservableProperty]
    public ObservableCollection<TaskItem> _children = new();

    [ObservableProperty]
    private bool? _isSelected;

    [ObservableProperty]
    private int _id;

    public TaskItem(TaskItem? parentItem)
    {
        _parentItem = parentItem;
    }

    private bool IgnoreIsSelectedChange { get; set; }

    public void UpdateIsSelected()
    {
        IgnoreIsSelectedChange = true;

        if (Children.Count > 0)
        {
            // 计算当前节点的 IsSelected 状态
            IsSelected = Children.All(x => x.IsSelected == true)
                ? true
                : Children.All(x => x.IsSelected == false)
                    ? false
                    : (bool?)null;
        }

        // 通知父节点更新状态
        _parentItem?.UpdateIsSelected();


        IgnoreIsSelectedChange = false;
    }

    [ObservableProperty]
    private bool _isExpanded;
    public override string ToString() => Name;

    partial void OnIsSelectedChanged(bool? value)
    {
        if (IgnoreIsSelectedChange is true)
        {
            return;
        }

        foreach (TaskItem item in Children)
        {
            item.IsSelected = value;
        }

        _parentItem?.UpdateIsSelected();
    }
}


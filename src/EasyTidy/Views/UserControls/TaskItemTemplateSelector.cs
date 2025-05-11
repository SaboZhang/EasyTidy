using EasyTidy.Common.Model;

namespace EasyTidy.Views.UserControls;

public partial class TaskItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate RootTemplate { get; set; }
    public DataTemplate ChildTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is TaskItem taskItem && taskItem.IsRoot)
        {
            return RootTemplate;
        }
        return ChildTemplate;
    }
}

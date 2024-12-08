namespace EasyTidy.Common.Extensions;

public class StateTriggerExtensions : StateTriggerBase
{
    public static readonly DependencyProperty IsConditionMetProperty =
        DependencyProperty.Register(nameof(IsConditionMet), typeof(bool), typeof(StateTrigger), new PropertyMetadata(false));

    public bool IsConditionMet
    {
        get => (bool)GetValue(IsConditionMetProperty);
        set => SetValue(IsConditionMetProperty, value);
    }

    public string BindingPath { get; set; }

    public StateTriggerExtensions()
    {
        this.IsConditionMet = false;
    }

    public void CheckCondition(object sender, EventArgs e)
    {
        // 在这里通过绑定路径验证条件
        // 假设你使用的是简单的值检查，例如 IsNullOrEmpty
        if (string.IsNullOrEmpty(BindingPath))
        {
            this.IsConditionMet = true;
        }
        else
        {
            this.IsConditionMet = false;
        }
    }
}

using Microsoft.UI.Text;
using Microsoft.Xaml.Interactivity;

namespace EasyTidy.Behaviors;

public class RichEditBoxBehavior : Behavior<RichEditBox>
{
    public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(RichEditBoxBehavior),
                new PropertyMetadata(string.Empty, OnTextChangedFromViewModel));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnTextChangedFromViewModel(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RichEditBoxBehavior behavior && behavior.AssociatedObject != null && e.NewValue is string newText)
        {
            behavior.SetText(newText);
        }
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.TextChanged += OnTextChanged;
        AssociatedObject.TextCompositionStarted += OnCompositionStarted;
        AssociatedObject.TextCompositionEnded += OnCompositionCompleted;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.TextChanged -= OnTextChanged;
        AssociatedObject.TextCompositionStarted -= OnCompositionStarted;
        AssociatedObject.TextCompositionEnded -= OnCompositionCompleted;
    }

    private void OnCompositionStarted(RichEditBox sender, TextCompositionStartedEventArgs args)
    {
        IsComposing = true;
    }


    private void OnTextChanged(object sender, RoutedEventArgs e)
    {
        // 只在非输入法输入（如删除、回车）时更新
        if (!IsComposing)
        {
            UpdateText();
        }
    }

    private void OnCompositionCompleted(RichEditBox sender, TextCompositionEndedEventArgs args)
    {
        // 输入法输入完成后更新文本
        IsComposing = false;
        UpdateText();
    }

    private bool IsComposing { get; set; } = false;

    private void UpdateText()
    {
        AssociatedObject.Document.GetText(TextGetOptions.None, out string text);
        // Text = text.TrimEnd('\r', '\n'); // 移除多余的换行
        SetValue(TextProperty, text.TrimEnd('\r', '\n'));
    }

    private void SetText(string text)
    {
        AssociatedObject.Document.GetText(TextGetOptions.None, out string currentText);
        if (currentText != text)
        {
            AssociatedObject.Document.SetText(TextSetOptions.None, text);
        }

        // 强制光标移动到文本末尾
        var doc = AssociatedObject.Document;
        doc.Selection.StartPosition = doc.Selection.EndPosition = text.Length;

        // 确保输入方向正确
        AssociatedObject.FlowDirection = FlowDirection.LeftToRight;
        AssociatedObject.TextDocument.Selection.ParagraphFormat.Alignment = ParagraphAlignment.Left;
    }
}

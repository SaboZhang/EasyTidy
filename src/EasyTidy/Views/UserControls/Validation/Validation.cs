using System;
using System.Collections;
using System.ComponentModel;

namespace EasyTidy.Views.UserControls;

/// <summary>
/// Validation class that provides attached properties for data validation.
/// see <see cref="https://github.com/ghost1372/DevWinUI/tree/v8.2.0/dev/DevWinUI.Controls/Controls/Validation"/> 
/// </summary>
public sealed partial class Validation : DependencyObject
{
    public static readonly DependencyProperty ValidationProviderProperty
        = DependencyProperty.RegisterAttached("ValidationProvider", typeof(INotifyDataErrorInfo),
            typeof(Validation), new(null, OnValidationProviderChanged));

    public static readonly DependencyProperty ValidationPropertyNameProperty
        = DependencyProperty.RegisterAttached("ValidationPropertyName", typeof(string),
            typeof(Validation), null);

    public static readonly DependencyProperty ErrorsProperty
        = DependencyProperty.RegisterAttached("Errors", typeof(IEnumerable),
            typeof(Validation), null);

    public static readonly DependencyProperty ErrorTemplateProperty
        = DependencyProperty.RegisterAttached("ErrorTemplate", typeof(object),
            typeof(Validation), null);

    public static string GetValidationPropertyName(DependencyObject obj)
    {
        return (string)obj.GetValue(ValidationPropertyNameProperty);
    }

    public static void SetValidationPropertyName(DependencyObject obj, string value)
    {
        obj.SetValue(ValidationPropertyNameProperty, value);
    }

    public static IEnumerable GetErrors(DependencyObject obj)
    {
        return (IEnumerable)obj.GetValue(ErrorsProperty);
    }

    public static void SetErrors(DependencyObject obj, IEnumerable errors)
    {
        obj.SetValue(ErrorsProperty, errors);
    }

    public static object GetErrorTemplate(DependencyObject obj)
    {
        return obj.GetValue(ErrorTemplateProperty);
    }

    public static void SetErrorTemplate(DependencyObject obj, object value)
    {
        obj.SetValue(ErrorTemplateProperty, value);
    }

    public static INotifyDataErrorInfo GetValidationProvider(DependencyObject obj)
    {
        return (INotifyDataErrorInfo)obj.GetValue(ValidationProviderProperty);
    }

    public static void SetValidationProvider(DependencyObject obj, INotifyDataErrorInfo value)
    {
        obj.SetValue(ValidationProviderProperty, value);
    }

    private static void OnValidationProviderChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        sender.SetValue(ErrorsProperty, null);
        if (args.NewValue is INotifyDataErrorInfo info)
        {
            string propName = GetValidationPropertyName(sender);
            if (!string.IsNullOrEmpty(propName))
            {
                info.ErrorsChanged += (source, eventArgs) =>
                {
                    if (eventArgs.PropertyName == propName)
                        sender.SetValue(ErrorsProperty, info.GetErrors(propName));
                };

                sender.SetValue(ErrorsProperty, info.GetErrors(propName));
            }
        }
    }
}

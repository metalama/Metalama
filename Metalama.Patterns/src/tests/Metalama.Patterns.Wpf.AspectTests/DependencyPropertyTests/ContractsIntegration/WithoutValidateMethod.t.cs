using Metalama.Patterns.Contracts;
using System.Windows;
namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.ContractsIntegration;
internal class WithoutValidateMethod : DependencyObject
{
  [DependencyProperty]
  [Trim]
  public string Name
  {
    get
    {
      return (string)GetValue(NameProperty);
    }
    set
    {
      this.SetValue(NameProperty, value);
    }
  }
  public static readonly DependencyProperty NameProperty;
  static WithoutValidateMethod()
  {
    NameProperty = DependencyProperty.Register("Name", typeof(string), typeof(WithoutValidateMethod), new PropertyMetadata() { CoerceValueCallback = (d, value) =>
    {
      value = ApplyNameContracts((string)value);
      return value;
    } });
  }
  private static string ApplyNameContracts(string value)
  {
    value = value.Trim();
    return value;
  }
}
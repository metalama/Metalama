using Metalama.Patterns.Contracts;
using System.Windows;
namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.ContractsIntegration;
internal class WithValidateMethod : DependencyObject
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
  private void ValidateName(string name)
  {
    if (name.Length > 3)
    {
      throw new ArgumentOutOfRangeException();
    }
  }
  public static readonly DependencyProperty NameProperty;
  static WithValidateMethod()
  {
    NameProperty = DependencyProperty.Register("Name", typeof(string), typeof(WithValidateMethod), new PropertyMetadata() { CoerceValueCallback = (d, value) =>
    {
      value = ApplyNameContracts((string)value);
      ((WithValidateMethod)d).ValidateName((string)value);
      return value;
    } });
  }
  private static string ApplyNameContracts(string value)
  {
    value = value.Trim();
    return value;
  }
}
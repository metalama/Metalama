using Metalama.Patterns.Wpf;
using System.Windows;
namespace Metalama.Patterns.Contracts.DependencyPropertyTests.ReadOnly;
public class C : DependencyObject
{
  [DependencyProperty]
  public int TheReadOnlyProperty
  {
    get
    {
      return (int)GetValue(TheReadOnlyPropertyProperty);
    }
    private set
    {
      this.SetValue(TheReadOnlyPropertyPropertyKey, value);
    }
  }
  public void Increment()
  {
    this.TheReadOnlyProperty++;
  }
  public static readonly DependencyProperty TheReadOnlyPropertyProperty;
  private static readonly DependencyPropertyKey TheReadOnlyPropertyPropertyKey;
  static C()
  {
    TheReadOnlyPropertyPropertyKey = DependencyProperty.RegisterReadOnly("TheReadOnlyProperty", typeof(int), typeof(C), null);
    TheReadOnlyPropertyProperty = TheReadOnlyPropertyPropertyKey.DependencyProperty;
  }
}
using System.Windows;
namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.NoCallbacks;
public partial class NoCallbacks : DependencyObject
{
  [DependencyProperty]
  public int Foo
  {
    get
    {
      return (int)GetValue(FooProperty);
    }
    set
    {
      this.SetValue(FooProperty, value);
    }
  }
  public static readonly DependencyProperty FooProperty;
  static NoCallbacks()
  {
    FooProperty = DependencyProperty.Register("Foo", typeof(int), typeof(NoCallbacks));
  }
}
using System.Windows;
namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.StaticOnChangedDependencyProperty;
public partial class StaticOnChangedDependencyProperty : DependencyObject
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
  private static void OnFooChanged(DependencyProperty d)
  {
  }
  public static readonly DependencyProperty FooProperty;
  static StaticOnChangedDependencyProperty()
  {
    FooProperty = DependencyProperty.Register("Foo", typeof(int), typeof(StaticOnChangedDependencyProperty), new PropertyMetadata((d_1, e) => OnFooChanged(FooProperty)));
  }
}
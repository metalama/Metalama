using System.Windows;
namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.StaticOnChangedNoParameters;
public partial class StaticOnChangedNoParameters : DependencyObject
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
  private static void OnFooChanged()
  {
  }
  public static readonly DependencyProperty FooProperty;
  static StaticOnChangedNoParameters()
  {
    FooProperty = DependencyProperty.Register("Foo", typeof(int), typeof(StaticOnChangedNoParameters), new PropertyMetadata((d, e) => OnFooChanged()));
  }
}
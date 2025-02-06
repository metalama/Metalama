using System.Windows;
namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.InstanceOnChangedNoParameters;
public partial class InstanceOnChangedNoParameters : DependencyObject
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
  private void OnFooChanged()
  {
  }
  public static readonly DependencyProperty FooProperty;
  static InstanceOnChangedNoParameters()
  {
    FooProperty = DependencyProperty.Register("Foo", typeof(int), typeof(InstanceOnChangedNoParameters), new PropertyMetadata((d, e) => ((InstanceOnChangedNoParameters)d).OnFooChanged()));
  }
}
using System.Windows;
namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.PropertyInitializer.DefaultOptions;
public class DefaultOptions : DependencyObject
{
  private static List<int> InitMethod() => [1, 2, 3];
  [DependencyProperty]
  public List<int> Foo
  {
    get
    {
      return (List<int>)GetValue(FooProperty);
    }
    set
    {
      this.SetValue(FooProperty, value);
    }
  }
  public static readonly DependencyProperty FooProperty;
  static DefaultOptions()
  {
    FooProperty = DependencyProperty.Register("Foo", typeof(List<int>), typeof(DefaultOptions), new PropertyMetadata(InitMethod()));
  }
  public DefaultOptions()
  {
    Foo = InitMethod();
  }
}
using System.Windows;
namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.PropertyInitializer.InitializerProvidesDefaultValueIsTrueAndInitializerProvidesDefaultValueIsTrue;
public class InitializerProvidesDefaultValueIsTrueAndInitializerProvidesDefaultValueIsTrue : DependencyObject
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
  static InitializerProvidesDefaultValueIsTrueAndInitializerProvidesDefaultValueIsTrue()
  {
    FooProperty = DependencyProperty.Register("Foo", typeof(List<int>), typeof(InitializerProvidesDefaultValueIsTrueAndInitializerProvidesDefaultValueIsTrue), new PropertyMetadata(InitMethod()));
  }
  public InitializerProvidesDefaultValueIsTrueAndInitializerProvidesDefaultValueIsTrue()
  {
    Foo = InitMethod();
  }
}
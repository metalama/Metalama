using System.Windows;
namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.PropertyInitializer.InitializerProvidesDefaultValueIsFalseAndInitializerProvidesInitialValueIsTrue;
public class InitializerProvidesDefaultValueIsFalseAndInitializerProvidesInitialValueIsTrue : DependencyObject
{
  private static List<int> InitMethod() => [1, 2, 3];
  [DependencyProperty(InitializerProvidesDefaultValue = false)]
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
  static InitializerProvidesDefaultValueIsFalseAndInitializerProvidesInitialValueIsTrue()
  {
    FooProperty = DependencyProperty.Register("Foo", typeof(List<int>), typeof(InitializerProvidesDefaultValueIsFalseAndInitializerProvidesInitialValueIsTrue));
  }
  public InitializerProvidesDefaultValueIsFalseAndInitializerProvidesInitialValueIsTrue()
  {
    Foo = InitMethod();
  }
}
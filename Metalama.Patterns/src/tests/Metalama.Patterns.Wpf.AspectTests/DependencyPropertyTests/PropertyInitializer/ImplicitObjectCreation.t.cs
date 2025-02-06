using System.Windows;
namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.PropertyInitializer.ImplicitObjectCreation;
public class ImplicitObjectCreation : DependencyObject
{
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
  static ImplicitObjectCreation()
  {
    FooProperty = DependencyProperty.Register("Foo", typeof(List<int>), typeof(ImplicitObjectCreation), new PropertyMetadata((List<int>)[1, 2, 3]));
  }
  public ImplicitObjectCreation()
  {
    Foo = [1, 2, 3];
  }
}
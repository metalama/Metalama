using System.Windows;
namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.StaticValidateValue;
public partial class StaticValidateValue : DependencyObject
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
  private static void ValidateFoo(int value)
  {
  }
  [DependencyProperty]
  public List<int> AcceptsAssignable
  {
    get
    {
      return (List<int>)GetValue(AcceptsAssignableProperty);
    }
    set
    {
      this.SetValue(AcceptsAssignableProperty, value);
    }
  }
  private static void ValidateAcceptsAssignable(IEnumerable<int> value)
  {
  }
  [DependencyProperty]
  public int AcceptsGeneric
  {
    get
    {
      return (int)GetValue(AcceptsGenericProperty);
    }
    set
    {
      this.SetValue(AcceptsGenericProperty, value);
    }
  }
  private static void ValidateAcceptsGeneric<T>(T value)
  {
  }
  [DependencyProperty]
  public int AcceptsObject
  {
    get
    {
      return (int)GetValue(AcceptsObjectProperty);
    }
    set
    {
      this.SetValue(AcceptsObjectProperty, value);
    }
  }
  private static void ValidateAcceptsObject(object value)
  {
  }
  public static readonly DependencyProperty AcceptsAssignableProperty;
  public static readonly DependencyProperty AcceptsGenericProperty;
  public static readonly DependencyProperty AcceptsObjectProperty;
  public static readonly DependencyProperty FooProperty;
  static StaticValidateValue()
  {
    FooProperty = DependencyProperty.Register("Foo", typeof(int), typeof(StaticValidateValue), new PropertyMetadata() { CoerceValueCallback = (d, value_1) =>
    {
      ValidateFoo((int)value_1);
      return value_1;
    } });
    AcceptsAssignableProperty = DependencyProperty.Register("AcceptsAssignable", typeof(List<int>), typeof(StaticValidateValue), new PropertyMetadata() { CoerceValueCallback = (d_1, value_2) =>
    {
      ValidateAcceptsAssignable((List<int>)value_2);
      return value_2;
    } });
    AcceptsGenericProperty = DependencyProperty.Register("AcceptsGeneric", typeof(int), typeof(StaticValidateValue), new PropertyMetadata() { CoerceValueCallback = (d_2, value_3) =>
    {
      ValidateAcceptsGeneric((int)value_3);
      return value_3;
    } });
    AcceptsObjectProperty = DependencyProperty.Register("AcceptsObject", typeof(int), typeof(StaticValidateValue), new PropertyMetadata() { CoerceValueCallback = (d_3, value_4) =>
    {
      ValidateAcceptsObject(value_4);
      return value_4;
    } });
  }
}
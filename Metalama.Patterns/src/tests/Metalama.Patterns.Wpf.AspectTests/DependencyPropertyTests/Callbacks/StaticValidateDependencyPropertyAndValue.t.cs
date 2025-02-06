using System.Windows;
namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.StaticValidateDependencyPropertyAndValue;
public partial class StaticValidateDependencyPropertyAndValue : DependencyObject
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
  private static void ValidateFoo(DependencyProperty d, int value)
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
  private static void ValidateAcceptsAssignable(DependencyProperty d, IEnumerable<int> value)
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
  private static void ValidateAcceptsGeneric<T>(DependencyProperty d, T value)
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
  private static void ValidateAcceptsObject(DependencyProperty d, object value)
  {
  }
  public static readonly DependencyProperty AcceptsAssignableProperty;
  public static readonly DependencyProperty AcceptsGenericProperty;
  public static readonly DependencyProperty AcceptsObjectProperty;
  public static readonly DependencyProperty FooProperty;
  static StaticValidateDependencyPropertyAndValue()
  {
    FooProperty = DependencyProperty.Register("Foo", typeof(int), typeof(StaticValidateDependencyPropertyAndValue), new PropertyMetadata() { CoerceValueCallback = (d_1, value_1) =>
    {
      ValidateFoo(FooProperty, (int)value_1);
      return value_1;
    } });
    AcceptsAssignableProperty = DependencyProperty.Register("AcceptsAssignable", typeof(List<int>), typeof(StaticValidateDependencyPropertyAndValue), new PropertyMetadata() { CoerceValueCallback = (d_2, value_2) =>
    {
      ValidateAcceptsAssignable(AcceptsAssignableProperty, (List<int>)value_2);
      return value_2;
    } });
    AcceptsGenericProperty = DependencyProperty.Register("AcceptsGeneric", typeof(int), typeof(StaticValidateDependencyPropertyAndValue), new PropertyMetadata() { CoerceValueCallback = (d_3, value_3) =>
    {
      ValidateAcceptsGeneric(AcceptsGenericProperty, (int)value_3);
      return value_3;
    } });
    AcceptsObjectProperty = DependencyProperty.Register("AcceptsObject", typeof(int), typeof(StaticValidateDependencyPropertyAndValue), new PropertyMetadata() { CoerceValueCallback = (d_4, value_4) =>
    {
      ValidateAcceptsObject(AcceptsObjectProperty, value_4);
      return value_4;
    } });
  }
}
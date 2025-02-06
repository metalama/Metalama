using System.Windows;
namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.StaticValidateDependencyPropertyAndInstanceAndValue;
public partial class StaticValidateDependencyPropertyAndInstanceAndValue : DependencyObject
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
  private static void ValidateFoo(DependencyProperty d, StaticValidateDependencyPropertyAndInstanceAndValue instance, int value)
  {
  }
  [DependencyProperty]
  public List<int> AcceptsAssignableForValue
  {
    get
    {
      return (List<int>)GetValue(AcceptsAssignableForValueProperty);
    }
    set
    {
      this.SetValue(AcceptsAssignableForValueProperty, value);
    }
  }
  private static void ValidateAcceptsAssignableForValue(DependencyProperty d, StaticValidateDependencyPropertyAndInstanceAndValue instance, IEnumerable<int> value)
  {
  }
  [DependencyProperty]
  public int AcceptsGenericForValue
  {
    get
    {
      return (int)GetValue(AcceptsGenericForValueProperty);
    }
    set
    {
      this.SetValue(AcceptsGenericForValueProperty, value);
    }
  }
  private static void ValidateAcceptsGenericForValue<T>(DependencyProperty d, StaticValidateDependencyPropertyAndInstanceAndValue instance, T value)
  {
  }
  [DependencyProperty]
  public int AcceptsObjectForValue
  {
    get
    {
      return (int)GetValue(AcceptsObjectForValueProperty);
    }
    set
    {
      this.SetValue(AcceptsObjectForValueProperty, value);
    }
  }
  private static void ValidateAcceptsObjectForValue(DependencyProperty d, StaticValidateDependencyPropertyAndInstanceAndValue instance, object value)
  {
  }
  [DependencyProperty]
  public int AcceptsDependencyObjectForInstance
  {
    get
    {
      return (int)GetValue(AcceptsDependencyObjectForInstanceProperty);
    }
    set
    {
      this.SetValue(AcceptsDependencyObjectForInstanceProperty, value);
    }
  }
  private static void ValidateAcceptsDependencyObjectForInstance(DependencyProperty d, DependencyObject instance, int value)
  {
  }
  [DependencyProperty]
  public int AcceptsObjectForInstance
  {
    get
    {
      return (int)GetValue(AcceptsObjectForInstanceProperty);
    }
    set
    {
      this.SetValue(AcceptsObjectForInstanceProperty, value);
    }
  }
  private static void ValidateAcceptsObjectForInstance(DependencyProperty d, object instance, int value)
  {
  }
  public static readonly DependencyProperty AcceptsAssignableForValueProperty;
  public static readonly DependencyProperty AcceptsDependencyObjectForInstanceProperty;
  public static readonly DependencyProperty AcceptsGenericForValueProperty;
  public static readonly DependencyProperty AcceptsObjectForInstanceProperty;
  public static readonly DependencyProperty AcceptsObjectForValueProperty;
  public static readonly DependencyProperty FooProperty;
  static StaticValidateDependencyPropertyAndInstanceAndValue()
  {
    FooProperty = DependencyProperty.Register("Foo", typeof(int), typeof(StaticValidateDependencyPropertyAndInstanceAndValue), new PropertyMetadata() { CoerceValueCallback = (d_1, value_1) =>
    {
      ValidateFoo(FooProperty, (StaticValidateDependencyPropertyAndInstanceAndValue)d_1, (int)value_1);
      return value_1;
    } });
    AcceptsAssignableForValueProperty = DependencyProperty.Register("AcceptsAssignableForValue", typeof(List<int>), typeof(StaticValidateDependencyPropertyAndInstanceAndValue), new PropertyMetadata() { CoerceValueCallback = (d_2, value_2) =>
    {
      ValidateAcceptsAssignableForValue(AcceptsAssignableForValueProperty, (StaticValidateDependencyPropertyAndInstanceAndValue)d_2, (List<int>)value_2);
      return value_2;
    } });
    AcceptsGenericForValueProperty = DependencyProperty.Register("AcceptsGenericForValue", typeof(int), typeof(StaticValidateDependencyPropertyAndInstanceAndValue), new PropertyMetadata() { CoerceValueCallback = (d_3, value_3) =>
    {
      ValidateAcceptsGenericForValue(AcceptsGenericForValueProperty, (StaticValidateDependencyPropertyAndInstanceAndValue)d_3, (int)value_3);
      return value_3;
    } });
    AcceptsObjectForValueProperty = DependencyProperty.Register("AcceptsObjectForValue", typeof(int), typeof(StaticValidateDependencyPropertyAndInstanceAndValue), new PropertyMetadata() { CoerceValueCallback = (d_4, value_4) =>
    {
      ValidateAcceptsObjectForValue(AcceptsObjectForValueProperty, (StaticValidateDependencyPropertyAndInstanceAndValue)d_4, value_4);
      return value_4;
    } });
    AcceptsDependencyObjectForInstanceProperty = DependencyProperty.Register("AcceptsDependencyObjectForInstance", typeof(int), typeof(StaticValidateDependencyPropertyAndInstanceAndValue), new PropertyMetadata() { CoerceValueCallback = (d_5, value_5) =>
    {
      ValidateAcceptsDependencyObjectForInstance(AcceptsDependencyObjectForInstanceProperty, d_5, (int)value_5);
      return value_5;
    } });
    AcceptsObjectForInstanceProperty = DependencyProperty.Register("AcceptsObjectForInstance", typeof(int), typeof(StaticValidateDependencyPropertyAndInstanceAndValue), new PropertyMetadata() { CoerceValueCallback = (d_6, value_6) =>
    {
      ValidateAcceptsObjectForInstance(AcceptsObjectForInstanceProperty, d_6, (int)value_6);
      return value_6;
    } });
  }
}
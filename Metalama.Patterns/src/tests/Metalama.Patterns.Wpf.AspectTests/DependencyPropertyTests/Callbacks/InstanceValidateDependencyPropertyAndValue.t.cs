using System.Windows;
namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.InstanceValidateDependencyPropertyAndValue;
public partial class InstanceValidateDependencyPropertyAndValue : DependencyObject
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
  private void ValidateFoo(DependencyProperty d, int value) => throw new ArgumentException();
  [DependencyProperty]
  public List<int> AcceptAssignable
  {
    get
    {
      return (List<int>)GetValue(AcceptAssignableProperty);
    }
    set
    {
      this.SetValue(AcceptAssignableProperty, value);
    }
  }
  private void ValidateAcceptAssignable(DependencyProperty d, IEnumerable<int> value) => throw new ArgumentException();
  [DependencyProperty]
  public int AcceptGeneric
  {
    get
    {
      return (int)GetValue(AcceptGenericProperty);
    }
    set
    {
      this.SetValue(AcceptGenericProperty, value);
    }
  }
  private void ValidateAcceptGeneric<T>(DependencyProperty d, T value) => throw new ArgumentException();
  [DependencyProperty]
  public int AcceptObject
  {
    get
    {
      return (int)GetValue(AcceptObjectProperty);
    }
    set
    {
      this.SetValue(AcceptObjectProperty, value);
    }
  }
  private void ValidateAcceptObject(DependencyProperty d, object value) => throw new ArgumentException();
  public static readonly DependencyProperty AcceptAssignableProperty;
  public static readonly DependencyProperty AcceptGenericProperty;
  public static readonly DependencyProperty AcceptObjectProperty;
  public static readonly DependencyProperty FooProperty;
  static InstanceValidateDependencyPropertyAndValue()
  {
    FooProperty = DependencyProperty.Register("Foo", typeof(int), typeof(InstanceValidateDependencyPropertyAndValue), new PropertyMetadata() { CoerceValueCallback = (d_1, value_1) =>
    {
      ((InstanceValidateDependencyPropertyAndValue)d_1).ValidateFoo(FooProperty, (int)value_1);
      return value_1;
    } });
    AcceptAssignableProperty = DependencyProperty.Register("AcceptAssignable", typeof(List<int>), typeof(InstanceValidateDependencyPropertyAndValue), new PropertyMetadata() { CoerceValueCallback = (d_2, value_2) =>
    {
      ((InstanceValidateDependencyPropertyAndValue)d_2).ValidateAcceptAssignable(AcceptAssignableProperty, (List<int>)value_2);
      return value_2;
    } });
    AcceptGenericProperty = DependencyProperty.Register("AcceptGeneric", typeof(int), typeof(InstanceValidateDependencyPropertyAndValue), new PropertyMetadata() { CoerceValueCallback = (d_3, value_3) =>
    {
      ((InstanceValidateDependencyPropertyAndValue)d_3).ValidateAcceptGeneric(AcceptGenericProperty, (int)value_3);
      return value_3;
    } });
    AcceptObjectProperty = DependencyProperty.Register("AcceptObject", typeof(int), typeof(InstanceValidateDependencyPropertyAndValue), new PropertyMetadata() { CoerceValueCallback = (d_4, value_4) =>
    {
      ((InstanceValidateDependencyPropertyAndValue)d_4).ValidateAcceptObject(AcceptObjectProperty, value_4);
      return value_4;
    } });
  }
}
using System.Windows;
namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.InstanceValidateValue;
public partial class InstanceValidateValue : DependencyObject
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
  private void ValidateFoo(int value) => throw new ArgumentException();
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
  private void ValidateAcceptAssignable(IEnumerable<int> value) => throw new ArgumentException();
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
  private void ValidateAcceptGeneric<T>(T value) => throw new ArgumentException();
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
  private void ValidateAcceptObject(object value) => throw new ArgumentException();
  public static readonly DependencyProperty AcceptAssignableProperty;
  public static readonly DependencyProperty AcceptGenericProperty;
  public static readonly DependencyProperty AcceptObjectProperty;
  public static readonly DependencyProperty FooProperty;
  static InstanceValidateValue()
  {
    FooProperty = DependencyProperty.Register("Foo", typeof(int), typeof(InstanceValidateValue), new PropertyMetadata() { CoerceValueCallback = (d, value_1) =>
    {
      ((InstanceValidateValue)d).ValidateFoo((int)value_1);
      return value_1;
    } });
    AcceptAssignableProperty = DependencyProperty.Register("AcceptAssignable", typeof(List<int>), typeof(InstanceValidateValue), new PropertyMetadata() { CoerceValueCallback = (d_1, value_2) =>
    {
      ((InstanceValidateValue)d_1).ValidateAcceptAssignable((List<int>)value_2);
      return value_2;
    } });
    AcceptGenericProperty = DependencyProperty.Register("AcceptGeneric", typeof(int), typeof(InstanceValidateValue), new PropertyMetadata() { CoerceValueCallback = (d_2, value_3) =>
    {
      ((InstanceValidateValue)d_2).ValidateAcceptGeneric((int)value_3);
      return value_3;
    } });
    AcceptObjectProperty = DependencyProperty.Register("AcceptObject", typeof(int), typeof(InstanceValidateValue), new PropertyMetadata() { CoerceValueCallback = (d_3, value_4) =>
    {
      ((InstanceValidateValue)d_3).ValidateAcceptObject(value_4);
      return value_4;
    } });
  }
}
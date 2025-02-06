using System.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests.Generic2;
public interface IFoo
{
  int X { get; }
  int Y { get; }
}
[Observable]
public partial class D<T> : INotifyPropertyChanged where T : class, INotifyPropertyChanged, IFoo
{
  private T _d1 = default !;
  public T D1
  {
    get
    {
      return _d1;
    }
    set
    {
      if (!object.ReferenceEquals(value, _d1))
      {
        var oldValue = _d1;
        if (oldValue != null)
        {
          oldValue.PropertyChanged -= _handleD1PropertyChanged;
        }
        _d1 = value;
        OnObservablePropertyChanged("D1", oldValue, value);
        OnPropertyChanged("FooX");
        OnPropertyChanged("D1");
        SubscribeToD1(value);
      }
    }
  }
  public int FooX => this.D1.X;
  private PropertyChangedEventHandler? _handleD1PropertyChanged;
  [ObservedExpressions("D1")]
  protected virtual void OnChildPropertyChanged(string parentPropertyPath, string propertyName)
  {
  }
  [ObservedExpressions("D1")]
  protected virtual void OnObservablePropertyChanged(string propertyPath, INotifyPropertyChanged? oldValue, INotifyPropertyChanged? newValue)
  {
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  private void SubscribeToD1(T value)
  {
    if (value != null)
    {
      _handleD1PropertyChanged ??= HandlePropertyChanged;
      value.PropertyChanged += _handleD1PropertyChanged;
    }
    void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      {
        var propertyName = e.PropertyName;
        switch (propertyName)
        {
          case "X":
            OnPropertyChanged("FooX");
            OnChildPropertyChanged("D1", "X");
            break;
          default:
            OnChildPropertyChanged("D1", propertyName);
            break;
        }
      }
    }
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
public partial class DD<T> : D<T> where T : class, INotifyPropertyChanged, IFoo
{
  public int FooY => this.D1.Y;
  protected override void OnChildPropertyChanged(string parentPropertyPath, string propertyName)
  {
    switch (parentPropertyPath, propertyName)
    {
      case ("D1", "Y"):
        OnPropertyChanged("FooY");
        base.OnChildPropertyChanged(parentPropertyPath, propertyName);
        break;
    }
    base.OnChildPropertyChanged(parentPropertyPath, propertyName);
  }
  protected override void OnPropertyChanged(string propertyName)
  {
    switch (propertyName)
    {
      case "D1":
        OnPropertyChanged("FooY");
        break;
    }
    base.OnPropertyChanged(propertyName);
  }
}
[Observable]
public partial class MyFoo : IFoo, INotifyPropertyChanged
{
  private int _x;
  public int X
  {
    get
    {
      return _x;
    }
    set
    {
      if (_x != value)
      {
        _x = value;
        OnPropertyChanged("X");
      }
    }
  }
  private int _y;
  public int Y
  {
    get
    {
      return _y;
    }
    set
    {
      if (_y != value)
      {
        _y = value;
        OnPropertyChanged("Y");
      }
    }
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
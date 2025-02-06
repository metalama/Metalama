using System.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests.ChildPropertiesFourDeep;
[Observable]
public partial class A : INotifyPropertyChanged
{
  public A()
  {
    this.A2 = new B();
  }
  private int _a1;
  public int A1
  {
    get
    {
      return _a1;
    }
    set
    {
      if (_a1 != value)
      {
        _a1 = value;
        OnPropertyChanged("A1");
      }
    }
  }
  private B _a2 = default !;
  public B A2
  {
    get
    {
      return _a2;
    }
    set
    {
      if (!object.ReferenceEquals(value, _a2))
      {
        var oldValue = _a2;
        if (oldValue != null)
        {
          oldValue.PropertyChanged -= _handleA2PropertyChanged;
        }
        _a2 = value;
        OnObservablePropertyChanged("A2", oldValue, (INotifyPropertyChanged? )value);
        UpdateA2B2();
        OnPropertyChanged("A2");
        SubscribeToA2(value);
      }
    }
  }
  public int A3 => this.A2.B2.C2.D1;
  private PropertyChangedEventHandler? _handleA2B2C2PropertyChanged;
  private PropertyChangedEventHandler? _handleA2B2PropertyChanged;
  private PropertyChangedEventHandler? _handleA2PropertyChanged;
  private C? _lastA2B2;
  private D? _lastA2B2C2;
  [ObservedExpressions("A2", "A2.B2", "A2.B2.C2")]
  protected virtual void OnChildPropertyChanged(string parentPropertyPath, string propertyName)
  {
  }
  [ObservedExpressions("A2")]
  protected virtual void OnObservablePropertyChanged(string propertyPath, INotifyPropertyChanged? oldValue, INotifyPropertyChanged? newValue)
  {
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  private void SubscribeToA2(B value)
  {
    if (value != null)
    {
      _handleA2PropertyChanged ??= HandlePropertyChanged;
      value.PropertyChanged += _handleA2PropertyChanged;
    }
    void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      {
        var propertyName = e.PropertyName;
        switch (propertyName)
        {
          case "B2":
            UpdateA2B2();
            break;
          default:
            OnChildPropertyChanged("A2", propertyName);
            break;
        }
      }
    }
  }
  private void UpdateA2B2()
  {
    var newValue = A2?.B2;
    if (!object.ReferenceEquals(newValue, _lastA2B2))
    {
      if (!object.ReferenceEquals(_lastA2B2, null))
      {
        _lastA2B2.PropertyChanged -= _handleA2B2PropertyChanged;
      }
      if (newValue != null)
      {
        _handleA2B2PropertyChanged ??= HandleChildPropertyChanged;
        newValue.PropertyChanged += _handleA2B2PropertyChanged;
        void HandleChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
          {
            var propertyName = e.PropertyName;
            switch (propertyName)
            {
              case "C2":
                UpdateA2B2C2();
                break;
              default:
                OnChildPropertyChanged("A2.B2", propertyName);
                break;
            }
          }
        }
      }
      _lastA2B2 = newValue;
      UpdateA2B2C2();
      OnChildPropertyChanged("A2", "B2");
    }
  }
  private void UpdateA2B2C2()
  {
    var newValue = A2?.B2?.C2;
    if (!object.ReferenceEquals(newValue, _lastA2B2C2))
    {
      if (!object.ReferenceEquals(_lastA2B2C2, null))
      {
        _lastA2B2C2.PropertyChanged -= _handleA2B2C2PropertyChanged;
      }
      if (newValue != null)
      {
        _handleA2B2C2PropertyChanged ??= HandleChildPropertyChanged;
        newValue.PropertyChanged += _handleA2B2C2PropertyChanged;
        void HandleChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
          {
            var propertyName = e.PropertyName;
            switch (propertyName)
            {
              case "D1":
                OnPropertyChanged("A3");
                OnChildPropertyChanged("A2.B2.C2", "D1");
                break;
              default:
                OnChildPropertyChanged("A2.B2.C2", propertyName);
                break;
            }
          }
        }
      }
      _lastA2B2C2 = newValue;
      OnPropertyChanged("A3");
      OnChildPropertyChanged("A2.B2", "C2");
    }
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
[Observable]
public partial class B : INotifyPropertyChanged
{
  public B()
  {
    this.B2 = new C();
  }
  private int _b1;
  public int B1
  {
    get
    {
      return _b1;
    }
    set
    {
      if (_b1 != value)
      {
        _b1 = value;
        OnPropertyChanged("B1");
      }
    }
  }
  private C _b2 = default !;
  public C B2
  {
    get
    {
      return _b2;
    }
    set
    {
      if (!object.ReferenceEquals(value, _b2))
      {
        var oldValue = _b2;
        _b2 = value;
        OnObservablePropertyChanged("B2", (INotifyPropertyChanged? )oldValue, (INotifyPropertyChanged? )value);
        OnPropertyChanged("B2");
      }
    }
  }
  [ObservedExpressions("B2")]
  protected virtual void OnObservablePropertyChanged(string propertyPath, INotifyPropertyChanged? oldValue, INotifyPropertyChanged? newValue)
  {
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
[Observable]
public partial class C : INotifyPropertyChanged
{
  public C()
  {
    this.C2 = new D();
  }
  private int _c1;
  public int C1
  {
    get
    {
      return _c1;
    }
    set
    {
      if (_c1 != value)
      {
        _c1 = value;
        OnPropertyChanged("C1");
      }
    }
  }
  private D _c2 = default !;
  public D C2
  {
    get
    {
      return _c2;
    }
    set
    {
      if (!object.ReferenceEquals(value, _c2))
      {
        var oldValue = _c2;
        _c2 = value;
        OnObservablePropertyChanged("C2", (INotifyPropertyChanged? )oldValue, (INotifyPropertyChanged? )value);
        OnPropertyChanged("C2");
      }
    }
  }
  [ObservedExpressions("C2")]
  protected virtual void OnObservablePropertyChanged(string propertyPath, INotifyPropertyChanged? oldValue, INotifyPropertyChanged? newValue)
  {
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
[Observable]
public partial class D : INotifyPropertyChanged
{
  private int _d1;
  public int D1
  {
    get
    {
      return _d1;
    }
    set
    {
      if (_d1 != value)
      {
        _d1 = value;
        OnPropertyChanged("D1");
      }
    }
  }
  private int _d2;
  public int D2
  {
    get
    {
      return _d2;
    }
    set
    {
      if (_d2 != value)
      {
        _d2 = value;
        OnPropertyChanged("D2");
      }
    }
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
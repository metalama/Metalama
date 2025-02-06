using System.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests;
[Observable]
public partial class A<T> : INotifyPropertyChanged where T : class, INotifyPropertyChanged
{
  private T _a1 = default !;
  public T A1
  {
    get
    {
      return _a1;
    }
    set
    {
      if (!object.ReferenceEquals(value, _a1))
      {
        var oldValue = _a1;
        _a1 = value;
        OnObservablePropertyChanged("A1", (INotifyPropertyChanged? )oldValue, value);
        OnPropertyChanged("A1");
      }
    }
  }
  [ObservedExpressions("A1")]
  protected virtual void OnObservablePropertyChanged(string propertyPath, INotifyPropertyChanged? oldValue, INotifyPropertyChanged? newValue)
  {
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
public partial class AOfSimple : A<Simple>
{
  public int RefA1S1 => this.A1.S1;
  private PropertyChangedEventHandler? _handleA1PropertyChanged;
  [ObservedExpressions("A1")]
  protected virtual void OnChildPropertyChanged(string parentPropertyPath, string propertyName)
  {
  }
  protected override void OnObservablePropertyChanged(string propertyPath, INotifyPropertyChanged? oldValue, INotifyPropertyChanged? newValue)
  {
    if (propertyPath == "A1")
    {
      if (oldValue != null)
      {
        oldValue.PropertyChanged -= _handleA1PropertyChanged;
      }
      if (newValue != null)
      {
        _handleA1PropertyChanged ??= HandleChildPropertyChanged;
        newValue.PropertyChanged += _handleA1PropertyChanged;
        void HandleChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
          {
            var propertyName = e.PropertyName;
            switch (propertyName)
            {
              case "S1":
                OnPropertyChanged("RefA1S1");
                OnChildPropertyChanged("A1", "S1");
                break;
              default:
                OnChildPropertyChanged("A1", propertyName);
                break;
            }
          }
        }
      }
      OnPropertyChanged("RefA1S1");
    }
    base.OnObservablePropertyChanged(propertyPath, oldValue, newValue);
  }
  protected override void OnPropertyChanged(string propertyName)
  {
    base.OnPropertyChanged(propertyName);
  }
}
[Observable]
public partial class Simple : INotifyPropertyChanged
{
  private int _s1;
  public int S1
  {
    get
    {
      return _s1;
    }
    set
    {
      if (_s1 != value)
      {
        _s1 = value;
        OnPropertyChanged("S1");
      }
    }
  }
  private int _s2;
  public int S2
  {
    get
    {
      return _s2;
    }
    set
    {
      if (_s2 != value)
      {
        _s2 = value;
        OnPropertyChanged("S2");
      }
    }
  }
  private int _s3;
  public int S3
  {
    get
    {
      return _s3;
    }
    set
    {
      if (_s3 != value)
      {
        _s3 = value;
        OnPropertyChanged("S3");
      }
    }
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
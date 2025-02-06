using System.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests.IntermediateInterface_ExplicitImpl;
public interface IMyInterface : INotifyPropertyChanged
{
  int P { get; }
}
public class C : IMyInterface
{
  private event PropertyChangedEventHandler? PropertyChanged;
  event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged { add => this.PropertyChanged += value; remove => this.PropertyChanged -= value; }
  public int P => 0;
}
[Observable]
public class D : INotifyPropertyChanged
{
  private C _c = default !;
  public C C
  {
    get
    {
      return _c;
    }
    set
    {
      if (!object.ReferenceEquals(value, _c))
      {
        var oldValue = _c;
        if (oldValue != null)
        {
          ((INotifyPropertyChanged)oldValue).PropertyChanged -= _handleCPropertyChanged;
        }
        _c = value;
        OnObservablePropertyChanged("C", oldValue, value);
        OnPropertyChanged("P");
        OnPropertyChanged("C");
        SubscribeToC(value);
      }
    }
  }
  public int P => this.C.P;
  private PropertyChangedEventHandler? _handleCPropertyChanged;
  [ObservedExpressions("C")]
  protected virtual void OnChildPropertyChanged(string parentPropertyPath, string propertyName)
  {
  }
  [ObservedExpressions("C")]
  protected virtual void OnObservablePropertyChanged(string propertyPath, INotifyPropertyChanged? oldValue, INotifyPropertyChanged? newValue)
  {
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  private void SubscribeToC(C value)
  {
    if (value != null)
    {
      _handleCPropertyChanged ??= HandlePropertyChanged;
      ((INotifyPropertyChanged)value).PropertyChanged += _handleCPropertyChanged;
    }
    void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      {
        var propertyName = e.PropertyName;
        switch (propertyName)
        {
          case "P":
            OnPropertyChanged("P");
            OnChildPropertyChanged("C", "P");
            break;
          default:
            OnChildPropertyChanged("C", propertyName);
            break;
        }
      }
    }
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
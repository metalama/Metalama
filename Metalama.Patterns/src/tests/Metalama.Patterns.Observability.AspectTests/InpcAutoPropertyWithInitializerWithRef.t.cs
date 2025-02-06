using System.ComponentModel;
using Metalama.Patterns.Observability.AspectTests.Include;
namespace Metalama.Patterns.Observability.AspectTests.InpcAutoPropertyWithInitializerWithRef;
[Observable]
public class InpcAutoPropertyWithInitializerWithRef : INotifyPropertyChanged
{
  private SimpleInpcByHand _x = new(42);
  public SimpleInpcByHand X
  {
    get
    {
      return _x;
    }
    set
    {
      if (!object.ReferenceEquals(value, _x))
      {
        var oldValue = _x;
        if (oldValue != null)
        {
          oldValue.PropertyChanged -= _handleXPropertyChanged;
        }
        _x = value;
        OnObservablePropertyChanged("X", oldValue, value);
        OnPropertyChanged("Y");
        OnPropertyChanged("X");
        SubscribeToX(value);
      }
    }
  }
  public int Y => this.X.A;
  private PropertyChangedEventHandler? _handleXPropertyChanged;
  public InpcAutoPropertyWithInitializerWithRef()
  {
    SubscribeToX(X);
  }
  [ObservedExpressions("X")]
  protected virtual void OnChildPropertyChanged(string parentPropertyPath, string propertyName)
  {
  }
  [ObservedExpressions("X")]
  protected virtual void OnObservablePropertyChanged(string propertyPath, INotifyPropertyChanged? oldValue, INotifyPropertyChanged? newValue)
  {
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  private void SubscribeToX(SimpleInpcByHand value)
  {
    if (value != null)
    {
      _handleXPropertyChanged ??= HandlePropertyChanged;
      value.PropertyChanged += _handleXPropertyChanged;
    }
    void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      {
        var propertyName = e.PropertyName;
        switch (propertyName)
        {
          case "A":
            OnPropertyChanged("Y");
            OnChildPropertyChanged("X", "A");
            break;
          default:
            OnChildPropertyChanged("X", propertyName);
            break;
        }
      }
    }
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
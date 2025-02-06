[Observable]
public sealed class SealedInpcAutoPropertyWithRef : INotifyPropertyChanged
{
  private SimpleInpcByHand _x = default !;
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
        OnPropertyChanged("Y");
        OnPropertyChanged("X");
        SubscribeToX(value);
      }
    }
  }
  public int Y => this.X.A;
  private PropertyChangedEventHandler? _handleXPropertyChanged;
  [ObservedExpressions("X")]
  private void OnChildPropertyChanged(string parentPropertyPath, string propertyName)
  {
  }
  private void OnPropertyChanged(string propertyName)
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
[Observable]
public sealed class SealedWithChildObject : INotifyPropertyChanged
{
  private SimpleInpcByHand _child = default !;
  public SimpleInpcByHand Child
  {
    get
    {
      return _child;
    }
    set
    {
      if (!object.ReferenceEquals(value, _child))
      {
        var oldValue = _child;
        if (oldValue != null)
        {
          oldValue.PropertyChanged -= _handleChildPropertyChanged;
        }
        _child = value;
        OnPropertyChanged("DerivedFromChild");
        OnPropertyChanged("Child");
        SubscribeToChild(value);
      }
    }
  }
  public int DerivedFromChild => this.Child.A;
  private int _localProperty;
  public int LocalProperty
  {
    get
    {
      return _localProperty;
    }
    set
    {
      if (_localProperty != value)
      {
        _localProperty = value;
        OnPropertyChanged("LocalProperty");
      }
    }
  }
  private PropertyChangedEventHandler? _handleChildPropertyChanged;
  private void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  private void SubscribeToChild(SimpleInpcByHand value)
  {
    if (value != null)
    {
      _handleChildPropertyChanged ??= HandlePropertyChanged;
      value.PropertyChanged += _handleChildPropertyChanged;
    }
    void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      {
        var propertyName = e.PropertyName;
        switch (propertyName)
        {
          case "A":
            OnPropertyChanged("DerivedFromChild");
            break;
        }
      }
    }
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}

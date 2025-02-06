[Observable]
public class FieldBackedInpcProperty : INotifyPropertyChanged
{
  private A _x1 = default !;
  private A _x
  {
    get
    {
      return _x1;
    }
    set
    {
      if (!object.ReferenceEquals(value, _x1))
      {
        var oldValue = _x1;
        if (oldValue != null)
        {
          oldValue.PropertyChanged -= _handle_xPropertyChanged;
        }
        _x1 = value;
        OnPropertyChanged("P1");
        OnPropertyChanged("P2");
        SubscribeTo_x(value);
      }
    }
  }
  public A P1 => this._x;
  public int P2 => this._x.A1;
  private PropertyChangedEventHandler? _handle_xPropertyChanged;
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  private void SubscribeTo_x(A value)
  {
    if (value != null)
    {
      _handle_xPropertyChanged ??= HandlePropertyChanged;
      value.PropertyChanged += _handle_xPropertyChanged;
    }
    void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      {
        var propertyName = e.PropertyName;
        switch (propertyName)
        {
          case "A1":
            OnPropertyChanged("P2");
            break;
        }
      }
    }
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
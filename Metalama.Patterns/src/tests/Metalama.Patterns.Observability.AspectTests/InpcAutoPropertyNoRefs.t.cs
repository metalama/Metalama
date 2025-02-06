[Observable]
public class InpcAutoPropertyNoRefs : INotifyPropertyChanged
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
        _x = value;
        OnObservablePropertyChanged("X", (INotifyPropertyChanged? )oldValue, value);
        OnPropertyChanged("X");
      }
    }
  }
  [ObservedExpressions("X")]
  protected virtual void OnObservablePropertyChanged(string propertyPath, INotifyPropertyChanged? oldValue, INotifyPropertyChanged? newValue)
  {
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
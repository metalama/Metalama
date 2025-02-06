[Observable]
public sealed class SealedInpcAutoPropertyNoRefs : INotifyPropertyChanged
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
        _x = value;
        OnPropertyChanged("X");
      }
    }
  }
  private void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
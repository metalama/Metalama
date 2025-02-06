[Observable]
public class UsingFabricOnExternalClass : INotifyPropertyChanged
{
  public int Count => ExternalClass.Foo();
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
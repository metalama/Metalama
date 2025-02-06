[Observable]
public class UsingAttributeOnMethodOfOtherClass : INotifyPropertyChanged
{
  public int X => OtherClass.Foo();
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
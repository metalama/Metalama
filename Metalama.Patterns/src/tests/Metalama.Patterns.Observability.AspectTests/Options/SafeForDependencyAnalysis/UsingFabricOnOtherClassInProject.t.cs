[Observable]
public class UsingFabricOnOtherClassInProject : INotifyPropertyChanged
{
  public int X => OtherClass.Foo();
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
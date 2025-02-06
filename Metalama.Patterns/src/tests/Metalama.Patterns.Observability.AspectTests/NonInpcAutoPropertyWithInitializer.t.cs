using System.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests.NonInpcAutoPropertyWithInitializer;
[Observable]
public class NonInpcAutoPropertyWithInitializer : INotifyPropertyChanged
{
  private int _x = 42;
  public int X
  {
    get
    {
      return _x;
    }
    set
    {
      if (_x != value)
      {
        _x = value;
        OnPropertyChanged("X");
      }
    }
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
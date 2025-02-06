using System.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests;
[Observable]
public sealed class SealedNonInpcAutoProperty : INotifyPropertyChanged
{
  private int _x;
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
  private void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
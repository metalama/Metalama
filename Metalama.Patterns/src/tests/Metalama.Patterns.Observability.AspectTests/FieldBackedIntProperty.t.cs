using System.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests.FieldBackedIntProperty;
[Observable]
public class FieldBackedIntProperty : INotifyPropertyChanged
{
  private int _x1;
  private int _x
  {
    get
    {
      return _x1;
    }
    set
    {
      if (_x1 != value)
      {
        _x1 = value;
        OnPropertyChanged("X");
        OnPropertyChanged("Y");
      }
    }
  }
  public int X => this._x;
  public int Y => this.X;
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
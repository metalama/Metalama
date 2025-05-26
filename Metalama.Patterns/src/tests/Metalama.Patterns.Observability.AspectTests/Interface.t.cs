using System.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests.Interface;
[Observable]
public interface ITheInterface : INotifyPropertyChanged
{
  string TheProperty { get; set; }
}
public class TheClass : ITheInterface
{
  private string _theProperty = default !;
  public string TheProperty
  {
    get
    {
      return _theProperty;
    }
    set
    {
      if (!object.ReferenceEquals(value, _theProperty))
      {
        _theProperty = value;
        OnPropertyChanged("TheProperty");
      }
    }
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
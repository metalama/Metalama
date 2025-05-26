using System.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests.InterfaceExplicitImplementation;
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
  public event PropertyChangedEventHandler? PropertyChanged;
  protected virtual void OnPropertyChanged(string propertyName)
  {
    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}
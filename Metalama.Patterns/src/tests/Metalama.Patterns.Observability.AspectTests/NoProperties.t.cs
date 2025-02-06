using System.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests;
[Observable]
public class NoProperties : INotifyPropertyChanged
{
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
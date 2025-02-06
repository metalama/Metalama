using System.Collections.Generic;
using System.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests.Diagnostics;
[Observable]
public class PropertyOfUnconstrainedGenericType<T> : INotifyPropertyChanged
{
  private T _c1 = default !;
  public T C1
  {
    get
    {
      return _c1;
    }
    set
    {
      if (!EqualityComparer<T>.Default.Equals(value, _c1))
      {
        _c1 = value;
        OnPropertyChanged("C1");
      }
    }
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
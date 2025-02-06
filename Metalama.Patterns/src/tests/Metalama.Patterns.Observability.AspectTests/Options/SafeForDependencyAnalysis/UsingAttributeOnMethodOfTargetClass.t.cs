using System.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests.Options.IgnoreUnobservableExpressions.UsingAttributeOnMethodOfTargetClass;
[Observable]
public class UsingAttributeOnMethodOfTargetClass : INotifyPropertyChanged
{
  public int X => this.Foo();
  [Constant]
  private int Foo() => 42;
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
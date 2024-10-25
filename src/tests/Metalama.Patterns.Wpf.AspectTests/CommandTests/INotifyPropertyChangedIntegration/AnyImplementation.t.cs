using System.ComponentModel;
namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.INotifyPropertyChangedIntegration.AnyImplementation;
public class AnyImplementation : INotifyPropertyChanged
{
  public event PropertyChangedEventHandler? PropertyChanged;
  [Command]
  private void ExecuteFoo1()
  {
  }
  public bool CanExecuteFoo1 => true;
  public AnyImplementation()
  {
    Foo1Command = DelegateCommandFactory.CreateDelegateCommand(ExecuteFoo1, () => CanExecuteFoo1, this, "CanExecuteFoo1");
  }
  public DelegateCommand Foo1Command { get; }
}
public class ImplementedByBase : AnyImplementation
{
  [Command]
  private void ExecuteFoo2()
  {
  }
  public bool CanExecuteFoo2 => true;
  public ImplementedByBase()
  {
    Foo2Command = DelegateCommandFactory.CreateDelegateCommand(ExecuteFoo2, () => CanExecuteFoo2, this, "CanExecuteFoo2");
  }
  public DelegateCommand Foo2Command { get; }
}
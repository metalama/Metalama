using System.ComponentModel;
namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.INotifyPropertyChangedIntegration.Disabled;
// When EnableINotifyPropertyChangedIntegration is false, the command must not subscribe to PropertyChanged,
// and the LAMA5202 warning must not be reported for a non-public can-execute property.
public class Disabled : INotifyPropertyChanged
{
  public event PropertyChangedEventHandler? PropertyChanged;
  [Command(EnableINotifyPropertyChangedIntegration = false)]
  private void ExecuteFoo1()
  {
  }
  public bool CanExecuteFoo1 => true;
  [Command(EnableINotifyPropertyChangedIntegration = false)]
  private void ExecuteFoo2()
  {
  }
  private bool CanExecuteFoo2 => true;
  public Disabled()
  {
    Foo2Command = DelegateCommandFactory.CreateDelegateCommand(ExecuteFoo2, () => CanExecuteFoo2);
    Foo1Command = DelegateCommandFactory.CreateDelegateCommand(ExecuteFoo1, () => CanExecuteFoo1);
  }
  public DelegateCommand Foo1Command { get; }
  public DelegateCommand Foo2Command { get; }
}
namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;
public class ExplicitlyConfiguredByCommandAttribute
{
  [Command(CanExecuteMethod = nameof(SomeWeirdName1))]
  private void Exec1()
  {
  }
  private bool SomeWeirdName1() => true;
  [Command(CanExecuteMethod = nameof(CanExec1))]
  private void ExecuteConfiguredCanExecuteMethod()
  {
  }
  // Has the default can-execute name for Exec1() above, don't be fooled.
  private bool CanExec1() => true;
  [Command(CanExecuteProperty = nameof(CanExec2))]
  private void ExecuteConfiguredCanExecuteProperty()
  {
  }
  private bool CanExec2 => true;
  public ExplicitlyConfiguredByCommandAttribute()
  {
    Exec1Command = DelegateCommandFactory.CreateDelegateCommand(Exec1, SomeWeirdName1);
    ConfiguredCanExecuteMethodCommand = DelegateCommandFactory.CreateDelegateCommand(ExecuteConfiguredCanExecuteMethod, CanExec1);
    ConfiguredCanExecutePropertyCommand = DelegateCommandFactory.CreateDelegateCommand(ExecuteConfiguredCanExecuteProperty, () => CanExec2);
  }
  public DelegateCommand ConfiguredCanExecuteMethodCommand { get; }
  public DelegateCommand ConfiguredCanExecutePropertyCommand { get; }
  public DelegateCommand Exec1Command { get; }
}
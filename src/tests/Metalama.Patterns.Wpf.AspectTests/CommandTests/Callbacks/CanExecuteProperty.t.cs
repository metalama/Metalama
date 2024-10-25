namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;
public class CanExecuteProperty
{
  [Command]
  private void ExecuteInstance()
  {
  }
  private bool CanExecuteInstance => true;
  [Command]
  private static void ExecuteStatic()
  {
  }
  private static bool CanExecuteStatic => true;
  public CanExecuteProperty()
  {
    InstanceCommand = DelegateCommandFactory.CreateDelegateCommand(ExecuteInstance, () => CanExecuteInstance);
    StaticCommand = DelegateCommandFactory.CreateDelegateCommand(ExecuteStatic, () => CanExecuteStatic);
  }
  public DelegateCommand InstanceCommand { get; }
  public DelegateCommand StaticCommand { get; }
}
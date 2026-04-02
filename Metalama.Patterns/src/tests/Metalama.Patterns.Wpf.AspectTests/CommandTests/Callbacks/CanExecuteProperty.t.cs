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
    StaticCommand = DelegateCommandFactory.CreateDelegateCommand(ExecuteStatic, () => CanExecuteStatic);
    InstanceCommand = DelegateCommandFactory.CreateDelegateCommand(ExecuteInstance, () => CanExecuteInstance);
  }
  public DelegateCommand InstanceCommand { get; }
  public DelegateCommand StaticCommand { get; }
}
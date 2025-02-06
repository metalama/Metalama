namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;
public class CanExecutePropertyBackground
{
  [Command(Background = true)]
  private void ExecuteInstance()
  {
  }
  private bool CanExecuteInstance => true;
  [Command(Background = true)]
  private static void ExecuteStatic()
  {
  }
  private static bool CanExecuteStatic => true;
  public CanExecutePropertyBackground()
  {
    InstanceCommand = DelegateCommandFactory.CreateBackgroundDelegateCommand(ExecuteInstance, () => CanExecuteInstance, false);
    StaticCommand = DelegateCommandFactory.CreateBackgroundDelegateCommand(ExecuteStatic, () => CanExecuteStatic, false);
  }
  public AsyncDelegateCommand InstanceCommand { get; }
  public AsyncDelegateCommand StaticCommand { get; }
}
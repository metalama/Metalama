namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;
public class CanExecutePropertysync
{
  [Command]
  private Task ExecuteInstanceAsync() => Task.CompletedTask;
  private bool CanExecuteInstance => true;
  [Command]
  private static Task ExecuteStaticAsync() => Task.CompletedTask;
  private static bool CanExecuteStatic => true;
  public CanExecutePropertysync()
  {
    InstanceCommand = DelegateCommandFactory.CreateAsyncDelegateCommand(ExecuteInstanceAsync, () => CanExecuteInstance, false, false);
    StaticCommand = DelegateCommandFactory.CreateAsyncDelegateCommand(ExecuteStaticAsync, () => CanExecuteStatic, false, false);
  }
  public AsyncDelegateCommand InstanceCommand { get; }
  public AsyncDelegateCommand StaticCommand { get; }
}
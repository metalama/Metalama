namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;
public class CanExecuteMethodAsync
{
  [Command]
  private Task ExecuteInstanceNoParametersAsync() => Task.CompletedTask;
  private bool CanExecuteInstanceNoParameters() => true;
  [Command]
  private static Task ExecuteStaticNoParametersAsync() => Task.CompletedTask;
  private static bool CanExecuteStaticNoParameters() => true;
  [Command]
  private Task ExecuteInstanceWithParameterAsync(int v) => Task.CompletedTask;
  private bool CanExecuteInstanceWithParameter(int v) => true;
  [Command]
  private static Task ExecuteStaticWithParameterAsync(int v) => Task.CompletedTask;
  private static bool CanExecuteStaticWithParameter(int v) => true;
  public CanExecuteMethodAsync()
  {
    InstanceNoParametersCommand = DelegateCommandFactory.CreateAsyncDelegateCommand(ExecuteInstanceNoParametersAsync, CanExecuteInstanceNoParameters, false, false);
    StaticNoParametersCommand = DelegateCommandFactory.CreateAsyncDelegateCommand(ExecuteStaticNoParametersAsync, CanExecuteStaticNoParameters, false, false);
    InstanceWithParameterCommand = DelegateCommandFactory.CreateAsyncDelegateCommand<int>(ExecuteInstanceWithParameterAsync, CanExecuteInstanceWithParameter, false, false);
    StaticWithParameterCommand = DelegateCommandFactory.CreateAsyncDelegateCommand<int>(ExecuteStaticWithParameterAsync, CanExecuteStaticWithParameter, false, false);
  }
  public AsyncDelegateCommand InstanceNoParametersCommand { get; }
  public AsyncDelegateCommand<int> InstanceWithParameterCommand { get; }
  public AsyncDelegateCommand StaticNoParametersCommand { get; }
  public AsyncDelegateCommand<int> StaticWithParameterCommand { get; }
}
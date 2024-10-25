namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;
public class ExecuteMethodAsync
{
  [Command]
  private Task ExecuteInstanceNoParametersAsync() => Task.CompletedTask;
  [Command]
  private static Task ExecuteStaticNoParametersAsync() => Task.CompletedTask;
  [Command]
  private Task ExecuteInstanceWithParameterAsync(int v) => Task.CompletedTask;
  [Command]
  private static Task ExecuteStaticWithParameterAsync(int v) => Task.CompletedTask;
  [Command]
  private Task ExecuteInstanceWithCancellationTokenAsync(CancellationToken cancellationToken) => Task.CompletedTask;
  [Command]
  private static Task ExecuteStaticWithCancellationTokenAsync(CancellationToken cancellationToken) => Task.CompletedTask;
  [Command]
  private Task ExecuteInstanceWithCancellationTokenAndParameterAsync(int v, CancellationToken cancellationToken) => Task.CompletedTask;
  [Command]
  private static Task ExecuteStaticWithCancellationTokenAndParameterAsync(int v, CancellationToken cancellationToken) => Task.CompletedTask;
  public ExecuteMethodAsync()
  {
    InstanceNoParametersCommand = DelegateCommandFactory.CreateAsyncDelegateCommand(ExecuteInstanceNoParametersAsync, null, false, false);
    StaticNoParametersCommand = DelegateCommandFactory.CreateAsyncDelegateCommand(ExecuteStaticNoParametersAsync, null, false, false);
    InstanceWithParameterCommand = DelegateCommandFactory.CreateAsyncDelegateCommand<int>(ExecuteInstanceWithParameterAsync, null, false, false);
    StaticWithParameterCommand = DelegateCommandFactory.CreateAsyncDelegateCommand<int>(ExecuteStaticWithParameterAsync, null, false, false);
    InstanceWithCancellationTokenCommand = DelegateCommandFactory.CreateAsyncDelegateCommand(ExecuteInstanceWithCancellationTokenAsync, null, false, false);
    StaticWithCancellationTokenCommand = DelegateCommandFactory.CreateAsyncDelegateCommand(ExecuteStaticWithCancellationTokenAsync, null, false, false);
    InstanceWithCancellationTokenAndParameterCommand = DelegateCommandFactory.CreateAsyncDelegateCommand<int>(ExecuteInstanceWithCancellationTokenAndParameterAsync, null, false, false);
    StaticWithCancellationTokenAndParameterCommand = DelegateCommandFactory.CreateAsyncDelegateCommand<int>(ExecuteStaticWithCancellationTokenAndParameterAsync, null, false, false);
  }
  public AsyncDelegateCommand InstanceNoParametersCommand { get; }
  public AsyncDelegateCommand<int> InstanceWithCancellationTokenAndParameterCommand { get; }
  public AsyncDelegateCommand InstanceWithCancellationTokenCommand { get; }
  public AsyncDelegateCommand<int> InstanceWithParameterCommand { get; }
  public AsyncDelegateCommand StaticNoParametersCommand { get; }
  public AsyncDelegateCommand<int> StaticWithCancellationTokenAndParameterCommand { get; }
  public AsyncDelegateCommand StaticWithCancellationTokenCommand { get; }
  public AsyncDelegateCommand<int> StaticWithParameterCommand { get; }
}
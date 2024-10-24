using Metalama.Patterns.Wpf.Implementation;
namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;
public class ExecuteMethodAsync
{
    [Command]
    private Task ExecuteInstanceNoParametersAsync() => Task.CompletedTask;
    [Command]
    private static Task ExecuteStaticNoParametersAsync() => Task.CompletedTask;
    [Command]
    private Task ExecuteInstanceWithParameterAsync( int v ) => Task.CompletedTask;
    [Command]
    private static Task ExecuteStaticWithParameterAsync( int v ) => Task.CompletedTask;
    [Command]
    private Task ExecuteInstanceWithCancellationTokenAsync( CancellationToken cancellationToken ) => Task.CompletedTask;
    [Command]
    private static Task ExecuteStaticWithCancellationTokenAsync( CancellationToken cancellationToken ) => Task.CompletedTask;
    [Command]
    private Task ExecuteInstanceWithCancellationTokenAndParameterAsync( int v, CancellationToken cancellationToken ) => Task.CompletedTask;
    [Command]
    private static Task ExecuteStaticWithCancellationTokenAndParameterAsync( int v, CancellationToken cancellationToken ) => Task.CompletedTask;
  public ExecuteMethodAsync()
    {
    InstanceNoParametersCommand = new AsyncDelegateCommand((_, _) =>
    {
      return ExecuteInstanceNoParametersAsync();
    }, null, false, false);
    StaticNoParametersCommand = new AsyncDelegateCommand((_, _) =>
            {
                return ExecuteStaticNoParametersAsync();
    }, null, false, false);
    InstanceWithParameterCommand = new AsyncDelegateCommand((arg, _) =>
    {
      return ExecuteInstanceWithParameterAsync((int)arg);
    }, null, false, false);
    StaticWithParameterCommand = new AsyncDelegateCommand((arg_1, _) =>
            {
                return ExecuteStaticWithParameterAsync( (int) arg_1 );
    }, null, false, false);
    InstanceWithCancellationTokenCommand = new AsyncDelegateCommand((_, ct) =>
    {
      return ExecuteInstanceWithCancellationTokenAsync(ct);
    }, null, true, false);
    StaticWithCancellationTokenCommand = new AsyncDelegateCommand((_, ct_1) =>
            {
                return ExecuteStaticWithCancellationTokenAsync( ct_1 );
    }, null, true, false);
    InstanceWithCancellationTokenAndParameterCommand = new AsyncDelegateCommand((arg_2, ct_2) =>
    {
      return ExecuteInstanceWithCancellationTokenAndParameterAsync((int)arg_2, ct_2);
    }, null, true, false);
    StaticWithCancellationTokenAndParameterCommand = new AsyncDelegateCommand((arg_3, ct_3) =>
            {
                return ExecuteStaticWithCancellationTokenAndParameterAsync( (int) arg_3, ct_3 );
    }, null, true, false);
    }
  public IAsyncCommand InstanceNoParametersCommand { get; }
  public IAsyncCommand InstanceWithCancellationTokenAndParameterCommand { get; }
  public IAsyncCommand InstanceWithCancellationTokenCommand { get; }
  public IAsyncCommand InstanceWithParameterCommand { get; }
  public IAsyncCommand StaticNoParametersCommand { get; }
  public IAsyncCommand StaticWithCancellationTokenAndParameterCommand { get; }
  public IAsyncCommand StaticWithCancellationTokenCommand { get; }
  public IAsyncCommand StaticWithParameterCommand { get; }
}
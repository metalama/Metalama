namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;
public class ExecuteMethodBackground
{
  [Command(Background = true)]
  private void ExecuteInstanceNoParameters()
  {
  }
  [Command(Background = true)]
  private static void ExecuteStaticNoParameters()
  {
  }
  [Command(Background = true)]
  private void ExecuteInstanceWithParameter(int v)
  {
  }
  [Command(Background = true)]
  private static void ExecuteStaticWithParameter(int v)
  {
  }
  public ExecuteMethodBackground()
  {
    InstanceNoParametersCommand = DelegateCommandFactory.CreateBackgroundDelegateCommand(ExecuteInstanceNoParameters, null, false);
    StaticNoParametersCommand = DelegateCommandFactory.CreateBackgroundDelegateCommand(ExecuteStaticNoParameters, null, false);
    InstanceWithParameterCommand = DelegateCommandFactory.CreateBackgroundDelegateCommand<int>(ExecuteInstanceWithParameter, null, false);
    StaticWithParameterCommand = DelegateCommandFactory.CreateBackgroundDelegateCommand<int>(ExecuteStaticWithParameter, null, false);
  }
  public AsyncDelegateCommand InstanceNoParametersCommand { get; }
  public AsyncDelegateCommand<int> InstanceWithParameterCommand { get; }
  public AsyncDelegateCommand StaticNoParametersCommand { get; }
  public AsyncDelegateCommand<int> StaticWithParameterCommand { get; }
}
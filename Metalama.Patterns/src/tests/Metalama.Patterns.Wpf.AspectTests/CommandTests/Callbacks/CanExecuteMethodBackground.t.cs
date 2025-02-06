namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;
public class CanExecuteMethodBackground
{
  [Command(Background = true)]
  private void ExecuteInstanceNoParameters()
  {
  }
  private bool CanExecuteInstanceNoParameters() => true;
  [Command(Background = true)]
  private static void ExecuteStaticNoParameters()
  {
  }
  private static bool CanExecuteStaticNoParameters() => true;
  [Command(Background = true)]
  private void ExecuteInstanceWithParameter(int v)
  {
  }
  private bool CanExecuteInstanceWithParameter(int v) => true;
  [Command(Background = true)]
  private static void ExecuteStaticWithParameter(int v)
  {
  }
  private static bool CanExecuteStaticWithParameter(int v) => true;
  public CanExecuteMethodBackground()
  {
    InstanceNoParametersCommand = DelegateCommandFactory.CreateBackgroundDelegateCommand(ExecuteInstanceNoParameters, CanExecuteInstanceNoParameters, false);
    StaticNoParametersCommand = DelegateCommandFactory.CreateBackgroundDelegateCommand(ExecuteStaticNoParameters, CanExecuteStaticNoParameters, false);
    InstanceWithParameterCommand = DelegateCommandFactory.CreateBackgroundDelegateCommand<int>(ExecuteInstanceWithParameter, CanExecuteInstanceWithParameter, false);
    StaticWithParameterCommand = DelegateCommandFactory.CreateBackgroundDelegateCommand<int>(ExecuteStaticWithParameter, CanExecuteStaticWithParameter, false);
  }
  public AsyncDelegateCommand InstanceNoParametersCommand { get; }
  public AsyncDelegateCommand<int> InstanceWithParameterCommand { get; }
  public AsyncDelegateCommand StaticNoParametersCommand { get; }
  public AsyncDelegateCommand<int> StaticWithParameterCommand { get; }
}
namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;
public class CanExecuteMethod
{
  [Command]
  private void ExecuteInstanceNoParameters()
  {
  }
  private bool CanExecuteInstanceNoParameters() => true;
  [Command]
  private static void ExecuteStaticNoParameters()
  {
  }
  private static bool CanExecuteStaticNoParameters() => true;
  [Command]
  private void ExecuteInstanceWithParameter(int v)
  {
  }
  private bool CanExecuteInstanceWithParameter(int v) => true;
  [Command]
  private static void ExecuteStaticWithParameter(int v)
  {
  }
  private static bool CanExecuteStaticWithParameter(int v) => true;
  public CanExecuteMethod()
  {
    StaticWithParameterCommand = DelegateCommandFactory.CreateDelegateCommand<int>(ExecuteStaticWithParameter, CanExecuteStaticWithParameter);
    InstanceWithParameterCommand = DelegateCommandFactory.CreateDelegateCommand<int>(ExecuteInstanceWithParameter, CanExecuteInstanceWithParameter);
    StaticNoParametersCommand = DelegateCommandFactory.CreateDelegateCommand(ExecuteStaticNoParameters, CanExecuteStaticNoParameters);
    InstanceNoParametersCommand = DelegateCommandFactory.CreateDelegateCommand(ExecuteInstanceNoParameters, CanExecuteInstanceNoParameters);
  }
  public DelegateCommand InstanceNoParametersCommand { get; }
  public DelegateCommand<int> InstanceWithParameterCommand { get; }
  public DelegateCommand StaticNoParametersCommand { get; }
  public DelegateCommand<int> StaticWithParameterCommand { get; }
}
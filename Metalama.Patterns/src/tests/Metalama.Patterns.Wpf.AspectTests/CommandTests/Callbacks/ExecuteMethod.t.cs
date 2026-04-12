namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;
public class ExecuteMethod
{
  [Command]
  private void ExecuteInstanceNoParameters()
  {
  }
  [Command]
  private static void ExecuteStaticNoParameters()
  {
  }
  [Command]
  private void ExecuteInstanceWithParameter(int v)
  {
  }
  [Command]
  private static void ExecuteStaticWithParameter(int v)
  {
  }
  public ExecuteMethod()
  {
    StaticWithParameterCommand = DelegateCommandFactory.CreateDelegateCommand<int>(ExecuteStaticWithParameter, null);
    InstanceWithParameterCommand = DelegateCommandFactory.CreateDelegateCommand<int>(ExecuteInstanceWithParameter, null);
    StaticNoParametersCommand = DelegateCommandFactory.CreateDelegateCommand(ExecuteStaticNoParameters, null);
    InstanceNoParametersCommand = DelegateCommandFactory.CreateDelegateCommand(ExecuteInstanceNoParameters, null);
  }
  public DelegateCommand InstanceNoParametersCommand { get; }
  public DelegateCommand<int> InstanceWithParameterCommand { get; }
  public DelegateCommand StaticNoParametersCommand { get; }
  public DelegateCommand<int> StaticWithParameterCommand { get; }
}
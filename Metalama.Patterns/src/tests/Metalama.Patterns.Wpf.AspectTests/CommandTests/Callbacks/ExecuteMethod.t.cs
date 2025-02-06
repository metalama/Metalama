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
    InstanceNoParametersCommand = DelegateCommandFactory.CreateDelegateCommand(ExecuteInstanceNoParameters, null);
    StaticNoParametersCommand = DelegateCommandFactory.CreateDelegateCommand(ExecuteStaticNoParameters, null);
    InstanceWithParameterCommand = DelegateCommandFactory.CreateDelegateCommand<int>(ExecuteInstanceWithParameter, null);
    StaticWithParameterCommand = DelegateCommandFactory.CreateDelegateCommand<int>(ExecuteStaticWithParameter, null);
  }
  public DelegateCommand InstanceNoParametersCommand { get; }
  public DelegateCommand<int> InstanceWithParameterCommand { get; }
  public DelegateCommand StaticNoParametersCommand { get; }
  public DelegateCommand<int> StaticWithParameterCommand { get; }
}
// Warning BUG681A on `TargetClass`: `Type 'TargetClass' IsConvertibleTo IMyInterface: True`
// Warning BUG681B on `TargetClass`: `Type 'TargetClass' has IMyInterface in AllImplementedInterfaces: True`
[ImplementInterfaceAspect]
[CheckInterfaceAspect]
public partial class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug681.IMyInterface
{
  public void DoSomething()
  {
    global::System.Console.WriteLine("Introduced");
  }
}

[MyAspect]
public partial class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug718.IMyInterface
{
  public void Bar()
  {
    global::System.Console.WriteLine($"Overriding in {typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug718.TargetClass).Name}");
    Console.WriteLine("Original Bar");
    object result = null;
    return;
  }
  public void Foo()
  {
    global::System.Console.WriteLine("Interface member implementation");
  }
}

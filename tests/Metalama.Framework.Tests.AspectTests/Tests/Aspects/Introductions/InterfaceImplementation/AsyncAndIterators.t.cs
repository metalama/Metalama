[Introduction]
public class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.AsyncAndIterators.IInterface
{
  public async global::System.Collections.Generic.IAsyncEnumerable<global::System.Int32> AsyncIterator()
  {
    global::System.Console.WriteLine("Introduced");
    await global::System.Threading.Tasks.Task.Yield();
    yield return 42;
  }
  public async global::System.Threading.Tasks.Task<global::System.Int32> AsyncMethod()
  {
    global::System.Console.WriteLine("Introduced");
    await global::System.Threading.Tasks.Task.Yield();
    return (global::System.Int32)42;
  }
  public global::System.Collections.Generic.IEnumerable<global::System.Int32> Iterator()
  {
    global::System.Console.WriteLine("Introduced");
    yield return 42;
  }
}
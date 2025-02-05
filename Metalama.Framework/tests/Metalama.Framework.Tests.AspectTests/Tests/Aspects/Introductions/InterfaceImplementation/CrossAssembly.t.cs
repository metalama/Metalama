[Introduction]
public class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.CrossAssembly.IInterface
{
  public global::System.Int32 AutoProperty { get; set; }
  public global::System.Int32 Property
  {
    get
    {
      global::System.Console.WriteLine("Introduced");
      return (global::System.Int32)42;
    }
    set
    {
      global::System.Console.WriteLine("Introduced");
    }
  }
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
  public T Method<T>(T x)
  {
    global::System.Console.WriteLine("Introduced");
    return x;
  }
  public event global::System.EventHandler Event
  {
    add
    {
      global::System.Console.WriteLine("Introduced");
    }
    remove
    {
      global::System.Console.WriteLine("Introduced");
    }
  }
  public event global::System.EventHandler? EventField;
}
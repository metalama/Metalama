[IntroduceAspect]
public class Target : Interface, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.InterfaceImplementation_Implicit.IntroducedInterface
{
  public void VoidMethod()
  {
    global::System.Console.WriteLine("Override.");
    Console.WriteLine("Original");
    return;
  }
  public int Method()
  {
    global::System.Console.WriteLine("Override.");
    Console.WriteLine("Original");
    return 42;
  }
  public T GenericMethod<T>(T value)
  {
    global::System.Console.WriteLine("Override.");
    Console.WriteLine("Original");
    return value;
  }
  public T IntroducedGenericMethod<T>(T value)
  {
    global::System.Console.WriteLine("Override.");
    global::System.Console.WriteLine("Introduced");
    return value;
  }
  public global::System.Int32 IntroducedMethod()
  {
    global::System.Console.WriteLine("Override.");
    global::System.Console.WriteLine("Introduced");
    return (global::System.Int32)42;
  }
  public void IntroducedVoidMethod()
  {
    global::System.Console.WriteLine("Override.");
    global::System.Console.WriteLine("Introduced");
    return;
  }
//public async Task AsyncVoidMethod()
//{
//    Console.WriteLine("Original");
//    await Task.Yield();
//}
//public async Task<int> AsyncMethod()
//{
//    Console.WriteLine("Original");
//    await Task.Yield();
//    return 42;
//}
//public IEnumerable<int> IteratorMethod()
//{
//    Console.WriteLine("Original");
//    yield return 42;
//}
//public async IAsyncEnumerable<int> AsyncIteratorMethod()
//{
//    Console.WriteLine("Original");
//    await Task.Yield();
//    yield return 42;
//}
}
[IntroduceAspect]
public class Target : Interface, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.InterfaceImplementation_Explicit.IntroducedInterface
{
  void Interface.VoidMethod()
  {
    global::System.Console.WriteLine("Override.");
    Console.WriteLine("Original");
    return;
  }
  int Interface.Method()
  {
    global::System.Console.WriteLine("Override.");
    Console.WriteLine("Original");
    return 42;
  }
  T Interface.GenericMethod<T>(T value)
  {
    global::System.Console.WriteLine("Override.");
    Console.WriteLine("Original");
    return value;
  }
  T global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.InterfaceImplementation_Explicit.IntroducedInterface.IntroducedGenericMethod<T>(T value)
  {
    global::System.Console.WriteLine("Override.");
    global::System.Console.WriteLine("Introduced");
    return value;
  }
  global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.InterfaceImplementation_Explicit.IntroducedInterface.IntroducedMethod()
  {
    global::System.Console.WriteLine("Override.");
    global::System.Console.WriteLine("Introduced");
    return (global::System.Int32)42;
  }
  void global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.InterfaceImplementation_Explicit.IntroducedInterface.IntroducedVoidMethod()
  {
    global::System.Console.WriteLine("Override.");
    global::System.Console.WriteLine("Introduced");
    return;
  }
//async Task Interface.AsyncVoidMethod()
//{
//    Console.WriteLine("Original");
//    await Task.Yield();
//}
//async Task<int> Interface.AsyncMethod()
//{
//    Console.WriteLine("Original");
//    await Task.Yield();
//    return 42;
//}
//IEnumerable<int> Interface.IteratorMethod()
//{
//    Console.WriteLine("Original");
//    yield return 42;
//}
//async IAsyncEnumerable<int> Interface.AsyncIteratorMethod()
//{
//    Console.WriteLine("Original");
//    await Task.Yield();
//    yield return 42;
//}
}
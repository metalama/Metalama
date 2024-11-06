[Introduction]
public class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.MemberConflict_DifferentSignature_Explicit.IInterface
{
  public int Method(int x)
  {
    Console.WriteLine("This is original method.");
    return x;
  }
  global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.MemberConflict_DifferentSignature_Explicit.IInterface.Method()
  {
    global::System.Console.WriteLine("This is introduced interface method.");
    return (global::System.Int32)42;
  }
}
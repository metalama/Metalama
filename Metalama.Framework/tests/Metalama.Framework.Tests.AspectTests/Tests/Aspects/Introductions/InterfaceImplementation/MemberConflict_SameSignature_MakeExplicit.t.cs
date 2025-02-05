[Introduction]
public class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.MemberConflict_SameSignature_MakeExplicit.IInterface
{
  public int Method()
  {
    Console.WriteLine("This is original method.");
    return 0;
  }
  public int Property
  {
    get
    {
      Console.WriteLine("This is original property.");
      return 0;
    }
    set
    {
      Console.WriteLine("This is original property.");
    }
  }
  public event EventHandler Event
  {
    add
    {
      Console.WriteLine("This is original event.");
    }
    remove
    {
      Console.WriteLine("This is original event.");
    }
  }
  global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.MemberConflict_SameSignature_MakeExplicit.IInterface.Property
  {
    get
    {
      global::System.Console.WriteLine("This is introduced interface property.");
      return (global::System.Int32)42;
    }
    set
    {
      global::System.Console.WriteLine("This is introduced interface property.");
    }
  }
  global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.MemberConflict_SameSignature_MakeExplicit.IInterface.Method()
  {
    global::System.Console.WriteLine("This is introduced interface method.");
    return (global::System.Int32)42;
  }
  event global::System.EventHandler global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.MemberConflict_SameSignature_MakeExplicit.IInterface.Event
  {
    add
    {
      global::System.Console.WriteLine("This is introduced interface event.");
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced interface event.");
    }
  }
}
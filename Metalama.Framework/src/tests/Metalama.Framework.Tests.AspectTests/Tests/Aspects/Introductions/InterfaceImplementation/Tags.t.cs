[Introduction]
public class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Tags.IInterface1, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Tags.IInterface2
{
  public global::System.Int32 Property1
  {
    get
    {
      global::System.Console.WriteLine("This is introduced interface member with Tag TestValue_For_Interface1.");
      return (global::System.Int32)42;
    }
    set
    {
      global::System.Console.WriteLine("This is introduced interface member with Tag TestValue_For_Interface1.");
    }
  }
  public global::System.Int32 Property2
  {
    get
    {
      global::System.Console.WriteLine("This is introduced interface member with Tag TestValue_For_Interface2.");
      return (global::System.Int32)42;
    }
    set
    {
      global::System.Console.WriteLine("This is introduced interface member with Tag TestValue_For_Interface2.");
    }
  }
  public global::System.Int32 InterfaceMethod1()
  {
    global::System.Console.WriteLine("This is introduced interface member with Tag TestValue_For_Interface1.");
    return default(global::System.Int32);
  }
  public global::System.Int32 InterfaceMethod2()
  {
    global::System.Console.WriteLine("This is introduced interface member with Tag TestValue_For_Interface2.");
    return default(global::System.Int32);
  }
  public event global::System.EventHandler Event1
  {
    add
    {
      global::System.Console.WriteLine("This is introduced interface member with Tag TestValue_For_Interface1.");
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced interface member with Tag TestValue_For_Interface1.");
    }
  }
  public event global::System.EventHandler Event2
  {
    add
    {
      global::System.Console.WriteLine("This is introduced interface member with Tag TestValue_For_Interface2.");
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced interface member with Tag TestValue_For_Interface2.");
    }
  }
}
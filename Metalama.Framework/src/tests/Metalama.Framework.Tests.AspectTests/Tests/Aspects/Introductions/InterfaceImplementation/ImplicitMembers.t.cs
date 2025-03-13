[Introduction]
public class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ImplicitMembers.IInterface
{
  public global::System.Int32 AutoProperty { get; set; }
  public global::System.Int32 AutoProperty_PrivateSet { get; private set; }
  public global::System.Int32 Property
  {
    get
    {
      global::System.Console.WriteLine("This is introduced interface member.");
      return (global::System.Int32)42;
    }
    set
    {
      global::System.Console.WriteLine("This is introduced interface member.");
    }
  }
  public global::System.Int32 Property_PrivateSet
  {
    get
    {
      global::System.Console.WriteLine("This is introduced interface member.");
      return (global::System.Int32)42;
    }
    set
    {
      global::System.Console.WriteLine("This is introduced interface member.");
    }
  }
  public global::System.Int32 InterfaceMethod()
  {
    global::System.Console.WriteLine("This is introduced interface member.");
    return default(global::System.Int32);
  }
  public event global::System.EventHandler Event
  {
    add
    {
      global::System.Console.WriteLine("This is introduced interface member.");
    }
    remove
    {
      global::System.Console.WriteLine("This is introduced interface member.");
    }
  }
  public event global::System.EventHandler? EventField;
}
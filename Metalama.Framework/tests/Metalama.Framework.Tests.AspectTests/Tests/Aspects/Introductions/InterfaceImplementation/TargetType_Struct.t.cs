[IntroduceAspect]
public struct TargetStruct : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.TargetType_Struct.IInterface
{
  public int ExistingField;
  public int ExistingProperty { get; set; }
  public void ExistingMethod()
  {
    Console.WriteLine("Original struct member");
  }
  public TargetStruct()
  {
  }
  public global::System.Int32 AutoProperty { get; set; }
  public global::System.Int32 Property
  {
    get
    {
      global::System.Console.WriteLine("Introduced interface member");
      return (global::System.Int32)42;
    }
    set
    {
      global::System.Console.WriteLine("Introduced interface member");
    }
  }
  public void IntroducedMethod()
  {
    global::System.Console.WriteLine("Introduced interface member");
  }
  public event global::System.EventHandler? Event
  {
    add
    {
      global::System.Console.WriteLine("Introduced interface member");
    }
    remove
    {
      global::System.Console.WriteLine("Introduced interface member");
    }
  }
  public event global::System.EventHandler? EventField;
}
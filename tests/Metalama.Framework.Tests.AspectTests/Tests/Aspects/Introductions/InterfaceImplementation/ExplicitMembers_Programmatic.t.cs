[Introduction]
public class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_Programmatic.IInterface
{
  global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_Programmatic.IInterface.AutoProperty { get; set; }
  global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_Programmatic.IInterface.Property
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
  global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_Programmatic.IInterface.InterfaceMethod(global::System.Int32 i)
  {
    global::System.Console.WriteLine("This is introduced interface member.");
    return i;
  }
  event global::System.EventHandler? global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_Programmatic.IInterface.Event
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
  private event global::System.EventHandler? _eventField;
  event global::System.EventHandler? global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_Programmatic.IInterface.EventField
  {
    add
    {
      this._eventField += value;
    }
    remove
    {
      this._eventField -= value;
    }
  }
}
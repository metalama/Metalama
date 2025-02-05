[Introduction]
public class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers.IInterface
{
  global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers.IInterface.AutoProperty { get; set; }
  global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers.IInterface.Property
  {
    get
    {
      global::System.Console.WriteLine("This is introduced interface member.");
      return default(global::System.Int32);
    }
    set
    {
      global::System.Console.WriteLine("This is introduced interface member.");
    }
  }
  global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers.IInterface.InterfaceMethod()
  {
    global::System.Console.WriteLine("This is introduced interface member.");
    return default(global::System.Int32);
  }
  event global::System.EventHandler global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers.IInterface.Event
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
  event global::System.EventHandler? global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers.IInterface.EventField
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
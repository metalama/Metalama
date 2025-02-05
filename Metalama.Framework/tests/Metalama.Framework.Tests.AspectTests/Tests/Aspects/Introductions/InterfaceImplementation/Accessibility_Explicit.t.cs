[Introduction]
public class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Accessibility_Explicit.IInterface
{
  global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Accessibility_Explicit.IInterface.AutoProperty { get; set; }
  global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Accessibility_Explicit.IInterface.Property
  {
    get
    {
      return (global::System.Int32)42;
    }
    set
    {
    }
  }
  global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Accessibility_Explicit.IInterface.Property_ExpressionBody
  {
    get
    {
      return (global::System.Int32)42;
    }
  }
  global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Accessibility_Explicit.IInterface.Property_GetOnly
  {
    get
    {
      return (global::System.Int32)42;
    }
  }
  void global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Accessibility_Explicit.IInterface.Method()
  {
    global::System.Console.WriteLine("Introduced interface member");
  }
  event global::System.EventHandler? global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Accessibility_Explicit.IInterface.Event
  {
    add
    {
    }
    remove
    {
    }
  }
  private event global::System.EventHandler? _eventField;
  event global::System.EventHandler? global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Accessibility_Explicit.IInterface.EventField
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
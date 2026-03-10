[Introduction]
public class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Attributes.IInterface
{
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Attributes.TestAspectAttribute(null)]
  public global::System.Int32 AutoProperty {[global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Attributes.TestAspectAttribute("Getter")]
    get; [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Attributes.TestAspectAttribute("Setter")]
    set; }
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Attributes.TestAspectAttribute(null)]
  public global::System.Int32 Property
  {
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Attributes.TestAspectAttribute("Getter")]
    get
    {
      return (global::System.Int32)42;
    }
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Attributes.TestAspectAttribute("Setter")]
    set
    {
    }
  }
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Attributes.TestInterfaceAttribute(null)]
  public void Method()
  {
    global::System.Console.WriteLine("Introduced interface member");
  }
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Attributes.TestAspectAttribute(null)]
  public event global::System.EventHandler? Event
  {
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Attributes.TestAspectAttribute(null)]
    add
    {
    }
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Attributes.TestAspectAttribute(null)]
    remove
    {
    }
  }
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Attributes.TestAspectAttribute(null)]
  [method: global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Attributes.TestAspectAttribute(null)]
  public event global::System.EventHandler? EventField;
}
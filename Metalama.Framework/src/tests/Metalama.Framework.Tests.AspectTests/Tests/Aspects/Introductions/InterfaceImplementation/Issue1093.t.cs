[Parent]
internal partial class Foo : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Issue1093.IGotParent
{
  public Foo? Parent { get; set; }
  public EventHandler Event;
  public long Method() => 0;
  global::System.Object? global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Issue1093.IGotParent.Property
  {
    get
    {
      return null;
    }
  }
  global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Issue1093.IGotParent.Method()
  {
    return (global::System.Int32)1;
  }
  private event global::System.Action _event = default !;
  event global::System.Action global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Issue1093.IGotParent.Event
  {
    add
    {
      this._event += value;
    }
    remove
    {
      this._event -= value;
    }
  }
}
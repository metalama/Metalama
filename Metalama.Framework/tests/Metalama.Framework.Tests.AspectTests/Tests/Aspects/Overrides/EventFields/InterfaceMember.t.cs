[Override]
[Introduction]
internal class TargetClass : Interface, global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.InterfaceMember.IntroducedInterface
{
  private event EventHandler? _event;
  public event EventHandler? Event
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      this._event += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      this._event -= value;
    }
  }
  private event EventHandler? _initializerEvent = Foo;
  public event EventHandler? InitializerEvent
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      this._initializerEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      this._initializerEvent -= value;
    }
  }
  public static void Foo(object? sender, EventArgs args)
  {
  }
  public static void Bar(global::System.Object? sender, global::System.EventArgs args)
  {
  }
  private event global::System.EventHandler? _initializerIntroducedEvent = Bar;
  public event global::System.EventHandler? InitializerIntroducedEvent
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      this._initializerIntroducedEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      this._initializerIntroducedEvent -= value;
    }
  }
  private event global::System.EventHandler? _introducedEvent;
  public event global::System.EventHandler? IntroducedEvent
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      this._introducedEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      this._introducedEvent -= value;
    }
  }
  private event global::System.EventHandler? _explicitIntroducedEvent;
  event global::System.EventHandler? global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.InterfaceMember.IntroducedInterface.ExplicitIntroducedEvent
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      this._explicitIntroducedEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      this._explicitIntroducedEvent -= value;
    }
  }
  private event global::System.EventHandler? _initializerExplicitIntroducedEvent = Bar;
  event global::System.EventHandler? global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.InterfaceMember.IntroducedInterface.InitializerExplicitIntroducedEvent
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      this._initializerExplicitIntroducedEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      this._initializerExplicitIntroducedEvent -= value;
    }
  }
}
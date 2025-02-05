[Introduction]
[Override]
internal class TargetClass
{
  [EventOnly]
  public event EventHandler? Event
  {
    [MethodOnly]
    [param: ParamOnly]
    [return: ReturnValueOnly]
    add
    {
      global::System.Console.WriteLine("This is the overridden add.");
      this.Event_Source += value;
      this.Event_Source += value;
    }
    [MethodOnly]
    [param: ParamOnly]
    [return: ReturnValueOnly]
    remove
    {
      global::System.Console.WriteLine("This is the overridden remove.");
      this.Event_Source -= value;
      this.Event_Source -= value;
    }
  }
  private event EventHandler? Event_Source
  {
    add
    {
      Console.WriteLine("This is the original add.");
    }
    remove
    {
      Console.WriteLine("This is the original remove.");
    }
  }
  private event global::System.EventHandler? IntroducedEvent_Introduction
  {
    add
    {
      global::System.Console.WriteLine("This is the introduced add.");
      this.IntroducedEvent_Empty += value;
      this.IntroducedEvent_Empty += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the introduced remove.");
      this.IntroducedEvent_Empty -= value;
      this.IntroducedEvent_Empty -= value;
    }
  }
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Attributes_Uninlineable.EventOnlyAttribute]
  public event global::System.EventHandler? IntroducedEvent
  {
    [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Attributes_Uninlineable.MethodOnlyAttribute]
    [return: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Attributes_Uninlineable.ReturnValueOnlyAttribute]
    [param: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Attributes_Uninlineable.ParamOnlyAttribute]
    add
    {
      global::System.Console.WriteLine("This is the overridden add.");
      this.IntroducedEvent_Introduction += value;
      this.IntroducedEvent_Introduction += value;
    }
    [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Attributes_Uninlineable.MethodOnlyAttribute]
    [return: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Attributes_Uninlineable.ReturnValueOnlyAttribute]
    [param: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Attributes_Uninlineable.ParamOnlyAttribute]
    remove
    {
      global::System.Console.WriteLine("This is the overridden remove.");
      this.IntroducedEvent_Introduction -= value;
      this.IntroducedEvent_Introduction -= value;
    }
  }
  private event global::System.EventHandler? IntroducedEvent_Empty
  {
    add
    {
    }
    remove
    {
    }
  }
}
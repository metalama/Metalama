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
      Console.WriteLine("This is the original add.");
    }
    [MethodOnly]
    [param: ParamOnly]
    [return: ReturnValueOnly]
    remove
    {
      global::System.Console.WriteLine("This is the overridden remove.");
      Console.WriteLine("This is the original remove.");
    }
  }
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Attributes.EventOnlyAttribute]
  public event global::System.EventHandler? IntroducedEvent
  {
    [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Attributes.MethodOnlyAttribute]
    [return: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Attributes.ReturnValueOnlyAttribute]
    [param: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Attributes.ParamOnlyAttribute]
    add
    {
      global::System.Console.WriteLine("This is the overridden add.");
      global::System.Console.WriteLine("This is the introduced add.");
    }
    [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Attributes.MethodOnlyAttribute]
    [return: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Attributes.ReturnValueOnlyAttribute]
    [param: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.Attributes.ParamOnlyAttribute]
    remove
    {
      global::System.Console.WriteLine("This is the overridden remove.");
      global::System.Console.WriteLine("This is the introduced remove.");
    }
  }
}
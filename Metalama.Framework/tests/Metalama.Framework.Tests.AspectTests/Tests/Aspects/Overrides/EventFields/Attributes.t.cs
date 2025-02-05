[Introduction]
[Override]
internal class TargetClass
{
  private event EventHandler? _eventField;
  // TODO: It is expected that the field attributes is not properly moved to the backing field as Roslyn currently does not expose the backing field.
  [EventOnly]
  public event EventHandler? EventField
  {
    [MethodOnly]
    add
    {
      global::System.Console.WriteLine("This is the overridden add.");
      this._eventField += value;
    }
    [MethodOnly]
    remove
    {
      global::System.Console.WriteLine("This is the overridden remove.");
      this._eventField -= value;
    }
  }
  private event global::System.EventHandler? _introducedEventField;
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Attributes.EventOnlyAttribute]
  public event global::System.EventHandler? IntroducedEventField
  {
    [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Attributes.MethodOnlyAttribute]
    add
    {
      global::System.Console.WriteLine("This is the overridden add.");
      this._introducedEventField += value;
    }
    [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Attributes.MethodOnlyAttribute]
    remove
    {
      global::System.Console.WriteLine("This is the overridden remove.");
      this._introducedEventField -= value;
    }
  }
}
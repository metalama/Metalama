[Introduction]
[Override]
internal partial class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.InterfaceEventField.Interface
{
  public static void Foo(global::System.Object? sender, global::System.EventArgs args)
  {
  }
  private event global::System.EventHandler? _eventField = Foo;
  event global::System.EventHandler? global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.InterfaceEventField.Interface.EventField
  {
    add
    {
      global::System.Console.WriteLine("Overriden add.");
      this._eventField += value;
    }
    remove
    {
      global::System.Console.WriteLine("Overriden remove.");
      this._eventField -= value;
    }
  }
  private event global::System.EventHandler? _eventField_Default = default;
  event global::System.EventHandler? global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.InterfaceEventField.Interface.EventField_Default
  {
    add
    {
      global::System.Console.WriteLine("Overriden add.");
      this._eventField_Default += value;
    }
    remove
    {
      global::System.Console.WriteLine("Overriden remove.");
      this._eventField_Default -= value;
    }
  }
}
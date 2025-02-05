internal class TargetClass
{
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.NoProceed.OverrideAttribute]
  public global::System.Int32 Field
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      return default;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
    }
  }
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.NoProceed.OverrideAttribute]
  public global::System.Int32 StaticField
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      return default;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
    }
  }
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.NoProceed.OverrideAttribute]
  public global::System.Int32 InitializerField
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      return default;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
    }
  }
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.NoProceed.OverrideAttribute]
  public global::System.Int32 ReadOnlyField
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      return default;
    }
    private init
    {
      global::System.Console.WriteLine("Override.");
    }
  }
  public TargetClass()
  {
    ReadOnlyField = 42;
  }
}
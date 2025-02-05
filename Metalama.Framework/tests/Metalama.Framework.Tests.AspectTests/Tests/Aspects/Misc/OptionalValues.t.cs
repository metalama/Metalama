[OptionalValueType]
internal class Account
{
  public string? Name
  {
    get
    {
      return (global::System.String? )((global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.OptionalValues.Account.Optional)this.OptionalValues).Name.Value;
    }
    set
    {
      ((global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.OptionalValues.Account.Optional)this.OptionalValues).Name = new global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.OptionalValues.OptionalValue<global::System.String?>(value);
    }
  }
  public Account? Parent
  {
    get
    {
      return (global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.OptionalValues.Account? )((global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.OptionalValues.Account.Optional)this.OptionalValues).Parent.Value;
    }
    set
    {
      ((global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.OptionalValues.Account.Optional)this.OptionalValues).Parent = new global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.OptionalValues.OptionalValue<global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.OptionalValues.Account?>(value);
    }
  }
  // Currently Metalama cannot generate new classes, so we need to have
  // an empty class in the code.
  public class Optional
  {
    public global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.OptionalValues.OptionalValue<global::System.String?> Name { get; set; }
    public global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.OptionalValues.OptionalValue<global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.OptionalValues.Account?> Parent { get; set; }
  }
  public global::Metalama.Framework.Tests.AspectTests.Aspects.Misc.OptionalValues.Account.Optional OptionalValues { get; private set; } = new Optional();
}
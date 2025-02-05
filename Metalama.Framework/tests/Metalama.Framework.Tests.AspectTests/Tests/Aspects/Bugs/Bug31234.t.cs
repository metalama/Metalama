[Initializer]
public partial class TargetClass : BaseClass
{
  private readonly int _abstractBaseProperty;
  [OverrideAttribute]
  public override int AbstractBaseProperty
  {
    get
    {
      return this.AbstractBaseProperty_Override;
    }
  }
  private global::System.Int32 AbstractBaseProperty_Override
  {
    get
    {
      _ = this._abstractBaseProperty;
      return this._abstractBaseProperty;
    }
    init
    {
      this._abstractBaseProperty = value;
      this._abstractBaseProperty = value;
    }
  }
  public TargetClass()
  { // Should invoke the first override since it changes semantics of the original declaration.
    this.AbstractBaseProperty_Override = 42;
  }
}
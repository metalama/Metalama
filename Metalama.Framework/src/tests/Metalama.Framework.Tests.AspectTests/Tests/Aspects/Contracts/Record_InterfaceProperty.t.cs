internal record Target : I
{
  private string? _m;
  [NotNull]
  string? I.M
  {
    get
    {
      return this._m;
    }
    set
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException("Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Record_InterfaceProperty.I.M");
      }
      this._m = value;
    }
  }
  private string? _n = default;
  [NotNull]
  string? I.N
  {
    get
    {
      return this._n;
    }
    set
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException("Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Record_InterfaceProperty.I.N");
      }
      this._n = value;
    }
  }
}
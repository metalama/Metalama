internal class Target
{
  private global::System.String _q1 = default !;
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Field_In.NotNullAttribute]
  private global::System.String q
  {
    get
    {
      return this._q1;
    }
    set
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException();
      }
      this._q1 = value;
    }
  }
}
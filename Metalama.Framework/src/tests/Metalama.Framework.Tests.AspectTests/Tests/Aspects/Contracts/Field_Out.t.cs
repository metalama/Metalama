internal class Target
{
  private global::System.String _q1 = default !;
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Field_Out.NotNullAttribute]
  private global::System.String q
  {
    get
    {
      var returnValue = this._q1;
      if (returnValue == null)
      {
        throw new global::System.ArgumentNullException();
      }
      return returnValue;
    }
    set
    {
      this._q1 = value;
    }
  }
}
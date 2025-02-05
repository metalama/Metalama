internal class TargetClass
{
  private global::System.Int32 _field;
  [global::Metalama.Framework.IntegrationTests.Aspects.Misc.PseudoParameterIExpression.TestAttribute]
  public global::System.Int32 Field
  {
    get
    {
      return this._field;
    }
    set
    {
      global::System.Console.WriteLine(value);
    }
  }
}
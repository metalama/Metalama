[Aspect]
internal class Target
{
  class TestType
  {
    private global::System.Int32 _introducedField;
    public global::System.Int32 IntroducedField
    {
      get
      {
        global::System.Console.WriteLine("Override");
        return this._introducedField;
      }
      set
      {
        global::System.Console.WriteLine("Override");
        this._introducedField = value;
      }
    }
  }
}
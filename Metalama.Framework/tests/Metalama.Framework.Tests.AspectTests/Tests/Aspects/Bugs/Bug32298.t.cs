[Override]
public class C
{
  private void M()
  {
  }
  private global::System.Int32 _introducedField;
  public global::System.Int32 IntroducedField
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._introducedField;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._introducedField = value;
    }
  }
}
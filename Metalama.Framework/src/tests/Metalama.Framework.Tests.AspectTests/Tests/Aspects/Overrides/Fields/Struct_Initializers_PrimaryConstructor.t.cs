[Introduction]
internal struct TargetStruct(int x)
{
  private global::System.Int32 _field = x;
  public global::System.Int32 Field
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._field;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._field = value;
    }
  }
  private global::System.Int32 _introducedField = (global::System.Int32)x;
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
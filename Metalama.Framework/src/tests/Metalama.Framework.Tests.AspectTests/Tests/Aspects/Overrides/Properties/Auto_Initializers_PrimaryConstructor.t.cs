[Introduction]
internal class TargetClass(int x)
{
  private int _property = x;
  public int Property
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._property;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._property = value;
    }
  }
  private global::System.Int32 _introducedProperty = (global::System.Int32)x;
  public global::System.Int32 IntroducedProperty
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._introducedProperty;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._introducedProperty = value;
    }
  }
}
[Introduction]
internal class TargetClass
{
  public global::System.Int32 this[global::System.String y, global::System.Int32 x]
  {
    get
    {
      return (global::System.Int32)(y.Length + x);
    }
    set
    {
      var q = y.Length + x + value;
    }
  }
}
[Introduction]
internal class TargetClass
{
  public int this[int x, string y]
  {
    get
    {
      var q = x + y.ToString().Length;
      return x + y.ToString().Length;
    }
    set
    {
      var q = x + y.ToString().Length + value;
      var z = x + y.ToString() + value;
    }
  }
}
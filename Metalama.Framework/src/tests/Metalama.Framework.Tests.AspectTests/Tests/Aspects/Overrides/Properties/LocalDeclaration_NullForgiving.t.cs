internal class TargetCode
{
  private int _field;
  [Aspect]
  public int Property
  {
    get
    {
      global::System.Console.WriteLine("Before");
      global::System.Int32 result;
      result = _field;
      global::System.Console.WriteLine("After");
      return (global::System.Int32)result;
    }
    set
    {
      _field = value;
    }
  }
}

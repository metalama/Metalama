internal class TargetCode
{
  private int _field;
  [Aspect]
  public int Property
  {
    get
    {
      global::System.Console.WriteLine("Before");
      return _field;
    }
    set
    {
      _field = value;
    }
  }
}

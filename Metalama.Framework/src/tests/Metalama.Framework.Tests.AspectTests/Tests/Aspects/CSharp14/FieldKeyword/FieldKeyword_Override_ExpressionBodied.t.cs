[MakeSettableAspect]
internal class C
{
  private int _counter;
  public int ComputedValue
  {
    get
    {
      global::System.Console.WriteLine("Getting ComputedValue");
      return (global::System.Int32)_computedValue;
    }
  }
  public int ConstantValue
  {
    get
    {
      global::System.Console.WriteLine("Getting ConstantValue");
      return (global::System.Int32)_constantValue;
    }
  }
  private readonly global::System.Int32 _computedValue;
  private readonly global::System.Int32 _constantValue;
}

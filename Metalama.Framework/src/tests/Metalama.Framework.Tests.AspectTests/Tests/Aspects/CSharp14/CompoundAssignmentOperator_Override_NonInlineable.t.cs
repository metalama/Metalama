public class C
{
  private int _x;
  [TheAspect]
  public void operator +=(int value)
  {
    global::System.Console.WriteLine("Call 1.");
    this.op_AdditionAssignment_Source(value);
    global::System.Console.WriteLine("Call 2.");
    this.op_AdditionAssignment_Source(value);
    return;
  }
  private void op_AdditionAssignment_Source(int value)
  {
    this._x += value;
  }
  [TheAspect]
  public void operator ++()
  {
    global::System.Console.WriteLine("Call 1.");
    this.op_IncrementAssignment_Source();
    global::System.Console.WriteLine("Call 2.");
    this.op_IncrementAssignment_Source();
    return;
  }
  private void op_IncrementAssignment_Source()
  {
    this._x++;
  }
  public void M()
  {
    var c = new C();
    Console.WriteLine(c += 5);
  }
}
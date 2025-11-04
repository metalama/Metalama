[TheAspect]
public class C
{
  public void M0()
  {
    this._M0LastArgs = global::System.ValueTuple.Create();
    return;
  }
  public void M1(int a)
  {
    this._M1LastArgs = global::System.ValueTuple.Create(a);
    global::System.Console.WriteLine(this._M1LastArgs.Item1);
    return;
  }
  public void M2(int a1, string a2)
  {
    this._M2LastArgs = (a1, a2);
    global::System.Console.WriteLine(this._M2LastArgs.a1);
    global::System.Console.WriteLine(this._M2LastArgs.a2);
    return;
  }
  public void M9(int a1, string a2, long a3, int a4, string a5, long a6, int a7, string a8, long a9)
  {
    this._M9LastArgs = (a1, a2, a3, a4, a5, a6, a7, a8, a9);
    global::System.Console.WriteLine(this._M9LastArgs.a1);
    global::System.Console.WriteLine(this._M9LastArgs.a2);
    global::System.Console.WriteLine(this._M9LastArgs.a3);
    global::System.Console.WriteLine(this._M9LastArgs.a4);
    global::System.Console.WriteLine(this._M9LastArgs.a5);
    global::System.Console.WriteLine(this._M9LastArgs.a6);
    global::System.Console.WriteLine(this._M9LastArgs.a7);
    global::System.Console.WriteLine(this._M9LastArgs.a8);
    global::System.Console.WriteLine(this._M9LastArgs.a9);
    return;
  }
  private global::System.ValueTuple _M0LastArgs;
  private global::System.ValueTuple<global::System.Int32> _M1LastArgs;
  private (global::System.Int32 a1, global::System.String a2) _M2LastArgs;
  private (global::System.Int32 a1, global::System.String a2, global::System.Int64 a3, global::System.Int32 a4, global::System.String a5, global::System.Int64 a6, global::System.Int32 a7, global::System.String a8, global::System.Int64 a9) _M9LastArgs;
}
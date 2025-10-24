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
    return;
  }
  public void M2(int a1, string a2)
  {
    this._M2LastArgs = (a1, a2);
    return;
  }
  public void M9(int a1, string a2, long a3, int a4, string a5, long a6, int a7, string a8, long a9)
  {
    this._M9LastArgs = (a1, a2, a3, a4, a5, a6, a7, a8, a9);
    return;
  }
  private global::System.ValueTuple _M0LastArgs;
  private global::System.ValueTuple<global::System.Int32> _M1LastArgs;
  private (global::System.Int32, global::System.String) _M2LastArgs;
  private (global::System.Int32, global::System.String, global::System.Int64, global::System.Int32, global::System.String, global::System.Int64, global::System.Int32, global::System.String, global::System.Int64) _M9LastArgs;
}
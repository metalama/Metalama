public class C
{
  [MyAspect]
  public C(global::System.Int32 p = 15)
  {
  }
  public C(string s) : this()
  {
  }
}
public class D : C
{
  public D()
  {
  }
}
public class C
{
  [MyAspect]
  public C(global::System.DateTime p = default(global::System.DateTime))
  {
  }
  public C(string s) : this(System.DateTime.Now)
  {
  }
}
public class D : C
{
  public D() : base(System.DateTime.Now)
  {
  }
}
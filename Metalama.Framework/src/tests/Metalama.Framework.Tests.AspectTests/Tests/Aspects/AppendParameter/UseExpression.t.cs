public class C
{
  [MyAspect]
  public C([AspectGenerated] DateTime p = default)
  {
  }
  public C(string s) : this(DateTime.Now)
  {
  }
}
public class D : C
{
  public D() : base(DateTime.Now)
  {
  }
}
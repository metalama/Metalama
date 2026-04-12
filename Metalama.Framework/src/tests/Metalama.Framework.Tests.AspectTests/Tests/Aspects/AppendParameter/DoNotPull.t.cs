public class C
{
  [MyAspect]
  public C([AspectGenerated] int p = 15)
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
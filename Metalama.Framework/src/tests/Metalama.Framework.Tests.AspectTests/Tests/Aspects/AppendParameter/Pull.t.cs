public class C
{
  [MyAspect]
  public C([AspectGenerated] int p = 15)
  {
  }
  public C(string s, [AspectGenerated] int p = 20) : this(p)
  {
  }
}
public class D : C
{
  public D([AspectGenerated] int p = 20) : base(p)
  {
  }
}
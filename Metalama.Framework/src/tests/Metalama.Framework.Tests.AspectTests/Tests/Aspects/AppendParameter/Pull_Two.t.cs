public class C
{
  [Aspect1, Aspect2]
  public C([AspectGenerated] DateTime p2 = default, [AspectGenerated] int p1 = 15)
  {
  }
  public C(string s, [AspectGenerated] DateTime p2 = default, [AspectGenerated] int p1 = 20) : this(p2, p1: p1)
  {
  }
}
public class D : C
{
  public D([AspectGenerated] DateTime p2 = default) : base(p2)
  {
  }
}
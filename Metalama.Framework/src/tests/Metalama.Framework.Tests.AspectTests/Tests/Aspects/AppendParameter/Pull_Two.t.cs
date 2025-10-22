public class C
{
  [Aspect1, Aspect2]
  public C([global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.DateTime p2 = default(global::System.DateTime), [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Int32 p1 = 15)
  {
  }
  public C(string s, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.DateTime p2 = default(global::System.DateTime), [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Int32 p1 = 20) : this(p2, p1: p1)
  {
  }
}
public class D : C
{
  public D([global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.DateTime p2 = default) : base(p2)
  {
  }
}
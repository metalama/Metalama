public class C
{
  [MyAspect]
  public C(global::System.Int32 p = 15)
  {
  }
  public C(string s, global::System.Int32 p = 20) : this(p)
  {
  }
}
public class D : C
{
  public D([global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Int32 p = 20) : base(p)
  {
  }
}
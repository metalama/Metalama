[MyAspect]
public class A
{
  public A(int x, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Int32 p)
  {
    this.X = x;
  }
  public int X { get; set; }
}
public class C : A
{
  public C(int x, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Int32 p) : base(42, p)
  {
    this.Y = x;
  }
  public int Y { get; }
}
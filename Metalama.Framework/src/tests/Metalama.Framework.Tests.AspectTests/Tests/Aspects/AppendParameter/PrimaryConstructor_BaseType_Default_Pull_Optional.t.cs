[MyAspect]
public class A(string? x = null, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Int32 p = 15)
{
}
public class C(int x) : A(p: 51)
{
  public int Y { get; } = x;
}
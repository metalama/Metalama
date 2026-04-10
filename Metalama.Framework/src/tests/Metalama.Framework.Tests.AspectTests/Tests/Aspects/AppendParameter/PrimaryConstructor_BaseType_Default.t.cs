[MyAspect]
public class A([AspectGenerated] int p = 15)
{
}
public class C(int x) : A
{
  public int Y { get; } = x;
}
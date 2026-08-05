[MyAspect]
public class A
{
  public A([AspectGenerated] X p = default)
  {
  }
}
public class B : A
{
  public B([AspectGenerated] X p) : base(p)
  {
  }
}
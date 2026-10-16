[MyAspect]
public class A
{
  public A([AspectGenerated] DateTime creationTime)
  {
  }
  public A() : this(creationTime: default)
  {
  }
}
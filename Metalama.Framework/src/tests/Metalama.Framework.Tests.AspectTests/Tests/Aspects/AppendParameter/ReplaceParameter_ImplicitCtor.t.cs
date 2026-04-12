[MyAspect]
public class Base
{
  public Base([AspectGenerated] ICovariant<Base> service = null)
  {
  }
}
public class Derived : Base
{
  public Derived([AspectGenerated] ICovariant<Derived> service = null) : base(service)
  {
  }
}

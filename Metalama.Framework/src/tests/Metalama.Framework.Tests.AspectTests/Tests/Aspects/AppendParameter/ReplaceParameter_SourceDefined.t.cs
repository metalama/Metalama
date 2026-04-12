[MyAspect]
public class Base
{
  public Base(int x, [AspectGenerated] ICovariant<Base> service = default)
  {
    this.X = x;
  }
  public int X { get; }
}
public class Derived : Base
{
  public Derived(int x, ICovariant<object> service, [AspectGenerated] ICovariant<Derived> service1 = default) : base(x, service1)
  {
  }
}

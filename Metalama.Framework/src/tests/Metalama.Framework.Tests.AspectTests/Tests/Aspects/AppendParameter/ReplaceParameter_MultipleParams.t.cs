[MyAspect]
public class Base
{
  public Base(int x, [AspectGenerated] ICovariant<Base> service = default, [AspectGenerated] IService svc = default)
  {
    this.X = x;
  }
  public int X { get; }
}
public class Derived : Base
{
  public Derived(int x, string name, [AspectGenerated] ICovariant<Derived> service = default, [AspectGenerated] IService svc = default) : base(x, service, svc: svc)
  {
    this.Name = name;
  }
  public string Name { get; }
}
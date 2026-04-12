[MyAspect]
public class Base
{
  public Base(int x, [MyCustom][AspectGenerated] ICovariant<Base> service = default)
  {
    this.X = x;
  }
  public int X { get; }
}
public class Derived : Base
{
  public Derived(int x, string name, [AspectGenerated][MyCustom] ICovariant<Derived> service = default) : base(x, service)
  {
    this.Name = name;
  }
  public string Name { get; }
}
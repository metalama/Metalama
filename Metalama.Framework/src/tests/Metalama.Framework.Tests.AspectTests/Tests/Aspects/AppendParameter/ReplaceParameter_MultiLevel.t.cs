[MyAspect]
public class Base
{
  public Base(int x, [AspectGenerated] ICovariant<Base> service = default)
  {
    this.X = x;
  }
  public int X { get; }
}
public class Middle : Base
{
  public Middle(int x, string name, [AspectGenerated] ICovariant<Middle> service = default) : base(x, service)
  {
    this.Name = name;
  }
  public string Name { get; }
}
public class Derived : Middle
{
  public Derived(int x, string name, bool flag, [AspectGenerated] ICovariant<Derived> service = default) : base(x, name, service)
  {
    this.Flag = flag;
  }
  public bool Flag { get; }
}
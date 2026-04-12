[MyAspect]
public partial class Base
{
  public Base(int x)
  {
    this.X = x;
  }
  public int X { get; }
}
public partial class Derived : Base
{
  public Derived(int x, string name) : base(x)
  {
    this.Name = name;
  }
  public string Name { get; }
}

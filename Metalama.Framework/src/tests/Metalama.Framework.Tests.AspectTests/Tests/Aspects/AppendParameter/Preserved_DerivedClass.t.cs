[MyAspect]
public class Base
{
  public Base(int id, [AspectGenerated] DateTime creationTime)
  {
    this.Id = id;
  }
  public int Id { get; }
  public Base(int id) : this(id: id, creationTime: DateTime.Now)
  {
  }
}
public class Derived : Base
{
  public Derived(int id, string name, [AspectGenerated] DateTime creationTime) : base(id, creationTime)
  {
    this.Name = name;
  }
  public string Name { get; }
  public Derived(int id, string name) : this(id: id, name: name, creationTime: DateTime.Now)
  {
  }
}
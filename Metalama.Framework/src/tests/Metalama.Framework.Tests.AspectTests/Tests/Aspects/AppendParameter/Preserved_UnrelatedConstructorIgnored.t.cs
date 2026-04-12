[MyAspect]
public class A
{
  public A(int id, [AspectGenerated] DateTime creationTime)
  {
    this.Id = id;
  }
  // This ctor is not mutated by the advice — should be left unchanged and no forwarder created for it.
  public A(string name)
  {
    this.Name = name;
  }
  public int Id { get; }
  public string? Name { get; }
  [SourceCompatibilityConstructor]
  public A(int id) : this(id: id, creationTime: DateTime.Now)
  {
  }
}
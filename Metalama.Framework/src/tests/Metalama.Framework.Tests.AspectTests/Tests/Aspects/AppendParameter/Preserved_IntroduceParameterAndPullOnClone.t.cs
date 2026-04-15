[MyAspect]
public class A
{
  public A(int id, [AspectGenerated] DateTime creationTime)
  {
    this.Id = id;
  }
  public int Id { get; }
  public A(int id) : this(id: id, creationTime: default)
  {
  }
}
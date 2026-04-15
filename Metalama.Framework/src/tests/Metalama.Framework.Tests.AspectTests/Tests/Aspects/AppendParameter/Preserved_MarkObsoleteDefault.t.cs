[MyAspect]
public class A
{
  public A(int id, [AspectGenerated] DateTime creationTime)
  {
    this.Id = id;
  }
  public int Id { get; }
  [Obsolete]
  public A(int id) : this(id: id, creationTime: DateTime.Now)
  {
  }
}
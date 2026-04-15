[MyAspect]
public class A
{
  public A(int id, [AspectGenerated] DateTime creationTime)
  {
    this.Id = id;
  }
  public int Id { get; }
  [Obsolete("Use the DI-aware constructor instead.", false)]
  public A(int id) : this(id: id, creationTime: DateTime.Now)
  {
  }
}
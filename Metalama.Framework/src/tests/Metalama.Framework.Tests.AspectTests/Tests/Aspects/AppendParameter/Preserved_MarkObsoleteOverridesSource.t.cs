[MyAspect]
public class A
{
  [Obsolete("old")]
  public A(int id, [AspectGenerated] DateTime creationTime)
  {
    this.Id = id;
  }
  public int Id { get; }
  [Obsolete("new message", false)]
  public A(int id) : this(id: id, creationTime: DateTime.Now)
  {
  }
}
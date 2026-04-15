[MyAspect]
public class A
{
  public A(int id, [AspectGenerated] DateTime creationTime)
  {
    this.Id = id;
  }
  public int Id { get; }
  [Obsolete("This constructor has been removed.", true)]
  public A(int id) : this(id: id, creationTime: DateTime.Now)
  {
  }
}
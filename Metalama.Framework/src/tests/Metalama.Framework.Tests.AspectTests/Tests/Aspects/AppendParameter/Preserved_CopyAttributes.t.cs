[MyAspect]
public class A
{
  [Obsolete("old")]
  public A(int id, [AspectGenerated] DateTime creationTime)
  {
    this.Id = id;
  }
  public int Id { get; }
  [SourceCompatibilityConstructor]
  [Obsolete("old")]
  public A(int id) : this(id: id, creationTime: DateTime.Now)
  {
  }
}
[MyAspect]
public class A
{
  public A([AspectGenerated] DateTime creationTime)
  {
  }
  public A(int id, [AspectGenerated] DateTime creationTime)
  {
    this.Id = id;
  }
  public int Id { get; }
  public A() : this(creationTime: DateTime.Now)
  {
  }
}
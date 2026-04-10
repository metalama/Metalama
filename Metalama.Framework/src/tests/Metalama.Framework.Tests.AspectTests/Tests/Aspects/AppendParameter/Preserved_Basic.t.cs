[MyAspect]
public class A
{
  public A(int id, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.DateTime creationTime)
  {
    this.Id = id;
  }
  public int Id { get; }
  [global::Metalama.Framework.RunTime.AspectGeneratedForwardingConstructorAttribute]
  public A(global::System.Int32 id) : this(id: id, creationTime: global::System.DateTime.Now)
  {
  }
}
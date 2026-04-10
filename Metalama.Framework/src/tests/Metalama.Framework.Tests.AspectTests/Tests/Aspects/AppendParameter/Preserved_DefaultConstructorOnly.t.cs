[MyAspect]
public class A
{
  public A([global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.DateTime creationTime)
  {
  }
  public A(int id, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.DateTime creationTime)
  {
    this.Id = id;
  }
  public int Id { get; }
  [global::Metalama.Framework.RunTime.AspectGeneratedForwardingConstructorAttribute]
  public A() : this(creationTime: global::System.DateTime.Now)
  {
  }
}
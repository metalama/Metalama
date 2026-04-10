[MyAspect]
public class A
{
  public A(int id, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.DateTime creationTime)
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
  [global::Metalama.Framework.RunTime.AspectGeneratedForwardingConstructorAttribute]
  public A(global::System.Int32 id) : this(id: id, creationTime: global::System.DateTime.Now)
  {
  }
}
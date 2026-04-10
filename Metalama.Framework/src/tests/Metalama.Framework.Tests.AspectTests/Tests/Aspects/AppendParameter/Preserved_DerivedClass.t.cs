[MyAspect]
public class Base
{
  public Base(int id, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.DateTime creationTime)
  {
    this.Id = id;
  }
  public int Id { get; }
  [global::Metalama.Framework.RunTime.AspectGeneratedForwardingConstructorAttribute]
  public Base(global::System.Int32 id) : this(id: id, creationTime: global::System.DateTime.Now)
  {
  }
}
public class Derived : Base
{
  public Derived(int id, string name, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.DateTime creationTime) : base(id, creationTime)
  {
    this.Name = name;
  }
  public string Name { get; }
  [global::Metalama.Framework.RunTime.AspectGeneratedForwardingConstructorAttribute]
  public Derived(global::System.Int32 id, global::System.String name) : this(id: id, name: name, creationTime: global::System.DateTime.Now)
  {
  }
}
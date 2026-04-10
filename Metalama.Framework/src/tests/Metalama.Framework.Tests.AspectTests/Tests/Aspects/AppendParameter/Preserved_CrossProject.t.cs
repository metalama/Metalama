public class Derived : Base
{
  public Derived(int id, string name, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.DateTime creationTime = default(global::System.DateTime)) : base(id, creationTime)
  {
    this.Name = name;
  }
  public string Name { get; }
  [global::Metalama.Framework.RunTime.AspectGeneratedForwardingConstructorAttribute]
  public Derived(global::System.Int32 id, global::System.String name) : this(id: id, name: name, creationTime: default(global::System.DateTime))
  {
  }
}
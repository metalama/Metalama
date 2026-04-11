public class Derived : Base
{
  public Derived(int id, string name, [AspectGenerated] DateTime creationTime = default) : base(id, creationTime)
  {
    this.Name = name;
  }
  public string Name { get; }
  [SourceCompatibilityConstructor]
  [Obsolete("Use the new constructor.", false)]
  public Derived(int id, string name) : this(id: id, name: name, creationTime: default)
  {
  }
}
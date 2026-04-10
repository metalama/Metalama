[MyAspect]
public class Order
{
  public Order(int id, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.DateTime creationTime)
  {
    this.Id = id;
  }
  public Order(int id, string label, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.DateTime creationTime) : this(id, creationTime)
  {
    this.Label = label;
  }
  public int Id { get; }
  public string? Label { get; }
  [global::Metalama.Framework.RunTime.AspectGeneratedForwardingConstructorAttribute]
  public Order(global::System.Int32 id) : this(id: id, creationTime: global::System.DateTime.Now)
  {
  }
  [global::Metalama.Framework.RunTime.AspectGeneratedForwardingConstructorAttribute]
  public Order(global::System.Int32 id, global::System.String label) : this(id: id, label: label, creationTime: global::System.DateTime.Now)
  {
  }
}
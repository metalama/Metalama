[Timestamped]
[Tracked]
public class Order
{
  public Order(int id, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Guid traceId, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.DateTime creationTime)
  {
    this.Id = id;
  }
  public int Id { get; }
  [global::Metalama.Framework.RunTime.AspectGeneratedForwardingConstructorAttribute]
  public Order(global::System.Int32 id) : this(id: id, traceId: global::System.Guid.NewGuid(), creationTime: global::System.DateTime.Now)
  {
  }
}
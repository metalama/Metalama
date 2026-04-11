[Timestamped]
[Tracked]
public class Order
{
  public Order(int id, [AspectGenerated] Guid traceId, [AspectGenerated] DateTime creationTime)
  {
    this.Id = id;
  }
  public int Id { get; }
  [SourceCompatibilityConstructor]
  public Order(int id) : this(id: id, traceId: Guid.NewGuid(), creationTime: DateTime.Now)
  {
  }
}
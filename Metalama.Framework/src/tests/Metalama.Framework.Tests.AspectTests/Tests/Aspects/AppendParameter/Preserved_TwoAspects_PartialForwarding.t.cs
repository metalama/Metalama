[Timestamped]
[Tracked]
public class Order
{
  public Order([AspectGenerated] Guid traceId, [AspectGenerated] DateTime creationTime)
  {
  }
  public Order(int id, [AspectGenerated] Guid traceId, [AspectGenerated] DateTime creationTime)
  {
    this.Id = id;
  }
  public int Id { get; }
  public Order() : this(traceId: Guid.NewGuid(), creationTime: DateTime.Now)
  {
  }
  public Order(int id) : this(id: id, traceId: Guid.NewGuid(), creationTime: DateTime.Now)
  {
  }
}
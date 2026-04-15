[MyAspect]
public class Order
{
  public Order(int id, [AspectGenerated] DateTime creationTime)
  {
    this.Id = id;
  }
  public Order(int id, string label, [AspectGenerated] DateTime creationTime) : this(id, creationTime)
  {
    this.Label = label;
  }
  public int Id { get; }
  public string? Label { get; }
  public Order(int id) : this(id: id, creationTime: DateTime.Now)
  {
  }
  public Order(int id, string label) : this(id: id, label: label, creationTime: DateTime.Now)
  {
  }
}
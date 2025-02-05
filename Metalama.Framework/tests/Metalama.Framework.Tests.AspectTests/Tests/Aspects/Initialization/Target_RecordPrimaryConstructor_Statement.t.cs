[Aspect]
public record TargetRecord
{
  private readonly int x;
  public int Y { get; }
  public int Foo() => x;
  public TargetRecord()
  {
    this.x = 0;
    this.Y = 0;
    x = 13;
    Y = 27;
  }
}
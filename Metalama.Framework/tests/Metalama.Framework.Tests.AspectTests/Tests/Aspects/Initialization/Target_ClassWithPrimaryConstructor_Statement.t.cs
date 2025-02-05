[Aspect]
abstract class TargetCode : Base
{
  int x1;
  public new int x2;
  private readonly int x3;
  public required int x4;
  [MyAttribute]
  private protected int x5;
  int x6 { get; }
  int x7 { get; set; }
  public int x8 { get; private set; }
  public int x9 { get; protected set; }
  protected internal int x10 { get; init; }
  public required int x11 { get; init; }
  public virtual int x12 { get; }
  public override int x13 { get; }
  public sealed override int x14 { get; }
  new int x15 { get; }
  [MyAttribute]
  int x16 { get; }
  [field: MyAttribute]
  int x17 { get; }
  int x18, x19;
  public TargetCode()
  {
    x1 = 1;
    x2 = 2;
    x3 = 3;
    x4 = 4;
    x5 = 5;
    x6 = 6;
    x7 = 7;
    x8 = 8;
    x9 = 9;
    x10 = 10;
    x11 = 11;
    x12 = 12;
    x13 = 13;
    x14 = 14;
    x15 = 15;
    x16 = 16;
    x17 = 17;
    x18 = 18;
    x19 = 19;
  }
}
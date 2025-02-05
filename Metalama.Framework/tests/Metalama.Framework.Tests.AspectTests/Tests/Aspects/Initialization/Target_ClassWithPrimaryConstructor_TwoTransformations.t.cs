[Aspect]
internal abstract class TargetCode
{
  public int Property { get; }
  public TargetCode()
  {
    Property = 13;
    Property = 42;
  }
}
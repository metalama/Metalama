internal class TargetCode
{
  [Aspect]
  public int P { get => 42; }
  private global::System.String? PropertyBody()
  {
    return "=> 42";
  }
}
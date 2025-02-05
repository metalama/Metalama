internal class Aspect : OverrideMethodAspect
{
  public override dynamic? OverrideMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
public class Target
{
  [Aspect]
  public IEnumerable<int> Foo()
  {
    return (global::System.Collections.Generic.IEnumerable<global::System.Int32>)GetNumbers();
    global::System.Collections.Generic.IEnumerable<global::System.Int32> GetNumbers()
    {
      yield return 42;
    }
  }
}
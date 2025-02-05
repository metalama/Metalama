internal class TargetCode
{
  [Aspect]
  private void VoidMethod()
  {
    return;
    return;
  }
  [Aspect]
  private int IntMethod()
  {
    return (global::System.Int32)42;
    return default;
  }
}
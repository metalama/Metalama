internal class TargetClass
{
  [Override]
  private void PrivateMethod()
  {
    global::System.Console.WriteLine("This is the overriding method.");
    Console.WriteLine("This is the original method.");
    return;
  }
  [Override]
  private protected void PrivateProtectedMethod()
  {
    global::System.Console.WriteLine("This is the overriding method.");
    Console.WriteLine("This is the original method.");
    return;
  }
  [Override]
  protected void ProtectedMethod()
  {
    global::System.Console.WriteLine("This is the overriding method.");
    Console.WriteLine("This is the original method.");
    return;
  }
  [Override]
  internal void InternalMethod()
  {
    global::System.Console.WriteLine("This is the overriding method.");
    Console.WriteLine("This is the original method.");
    return;
  }
  [Override]
  protected internal void ProtectedInternalMethod()
  {
    global::System.Console.WriteLine("This is the overriding method.");
    Console.WriteLine("This is the original method.");
    return;
  }
  [Override]
  public void PublicMethod()
  {
    global::System.Console.WriteLine("This is the overriding method.");
    Console.WriteLine("This is the original method.");
    return;
  }
}
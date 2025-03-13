internal class TargetClass
{
  [InnerOverride]
  [OuterOverride]
  public void VoidMethod()
  {
    global::System.Console.WriteLine("This is the outer overriding template method.");
    global::System.Console.WriteLine("This is the inner overriding template method.");
    Console.WriteLine("This is the original method.");
    return;
  }
  [InnerOverride]
  [OuterOverride]
  public int Method(int x)
  {
    global::System.Console.WriteLine("This is the outer overriding template method.");
    global::System.Console.WriteLine("This is the inner overriding template method.");
    Console.WriteLine($"This is the original method.");
    return x;
  }
  [InnerOverride]
  [OuterOverride]
  public T? GenericMethod<T>(T? x)
  {
    global::System.Console.WriteLine("This is the outer overriding template method.");
    global::System.Console.WriteLine("This is the inner overriding template method.");
    Console.WriteLine("This is the original method.");
    return x;
  }
}
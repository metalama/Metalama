internal class TargetClass
{
  [InnerOverride]
  [OuterOverride]
  public static TargetClass operator +(TargetClass a, TargetClass b)
  {
    global::System.Console.WriteLine("This is the outer overriding template method.");
    global::System.Console.WriteLine("This is the inner overriding template method.");
    Console.WriteLine($"This is the original operator.");
    return new TargetClass();
  }
  [InnerOverride]
  [OuterOverride]
  public static TargetClass operator -(TargetClass a)
  {
    global::System.Console.WriteLine("This is the outer overriding template method.");
    global::System.Console.WriteLine("This is the inner overriding template method.");
    Console.WriteLine($"This is the original operator.");
    return new TargetClass();
  }
  [InnerOverride]
  [OuterOverride]
  public static explicit operator TargetClass(int x)
  {
    global::System.Console.WriteLine("This is the outer overriding template method.");
    global::System.Console.WriteLine("This is the inner overriding template method.");
    Console.WriteLine($"This is the original operator.");
    return new TargetClass();
  }
}
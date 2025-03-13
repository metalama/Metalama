[Introduction]
internal class TargetClass
{
  public static TargetClass operator -(TargetClass a)
  {
    global::System.Console.WriteLine($"Unary operator UnaryNegation({a})");
    Console.WriteLine("This is the original operator.");
    return new TargetClass();
  }
  public static TargetClass operator +(TargetClass a, TargetClass b)
  {
    global::System.Console.WriteLine($"Binary operator Addition({a}, {b})");
    Console.WriteLine("This is the original operator.");
    return new TargetClass();
  }
  public static explicit operator TargetClass(int a)
  {
    global::System.Console.WriteLine($"Conversion operator ExplicitConversion({a})");
    Console.WriteLine("This is the original operator.");
    return new TargetClass();
  }
}
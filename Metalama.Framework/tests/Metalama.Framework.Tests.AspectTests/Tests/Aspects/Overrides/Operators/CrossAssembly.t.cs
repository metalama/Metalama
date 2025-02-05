[Override]
[Introduction]
internal class TargetClass
{
  public static TargetClass operator +(TargetClass x, int y)
  {
    global::System.Console.WriteLine("Override");
    Console.WriteLine("Original.");
    return x;
  }
  public static TargetClass operator +(TargetClass x)
  {
    global::System.Console.WriteLine("Override");
    Console.WriteLine("Original.");
    return x;
  }
  public static implicit operator TargetClass(int y)
  {
    global::System.Console.WriteLine("Override");
    Console.WriteLine("Original.");
    return new TargetClass();
  }
  public static global::System.Int32 operator +(global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.CrossAssembly.TargetClass x, global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.CrossAssembly.TargetClass y)
  {
    global::System.Console.WriteLine("Override");
    global::System.Console.WriteLine($"Binary operator Addition({x}, {y})");
    return default(global::System.Int32);
  }
  public static explicit operator global::System.Int32(global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.CrossAssembly.TargetClass x)
  {
    global::System.Console.WriteLine("Override");
    global::System.Console.WriteLine($"Conversion operator ExplicitConversion({x})");
    return default(global::System.Int32);
  }
  public static global::System.Int32 operator -(global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.CrossAssembly.TargetClass x)
  {
    global::System.Console.WriteLine("Override");
    global::System.Console.WriteLine($"Unary operator UnaryNegation({x})");
    return default(global::System.Int32);
  }
}
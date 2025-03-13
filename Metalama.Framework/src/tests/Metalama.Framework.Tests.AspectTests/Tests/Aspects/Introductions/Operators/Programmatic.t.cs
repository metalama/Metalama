[Introduction]
internal class TargetClass
{
  public static global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Programmatic.TargetClass operator +(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Programmatic.TargetClass x, global::System.Int32 y)
  {
    global::System.Console.WriteLine($"Binary operator Addition({x}, {y})");
    return default(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Programmatic.TargetClass);
  }
  public static explicit operator global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Programmatic.TargetClass(global::System.Int32 x)
  {
    global::System.Console.WriteLine($"Conversion operator ExplicitConversion({x})");
    return default(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Programmatic.TargetClass);
  }
  public static global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Programmatic.TargetClass operator -(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Programmatic.TargetClass x)
  {
    global::System.Console.WriteLine($"Unary operator UnaryNegation({x})");
    return default(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Programmatic.TargetClass);
  }
}
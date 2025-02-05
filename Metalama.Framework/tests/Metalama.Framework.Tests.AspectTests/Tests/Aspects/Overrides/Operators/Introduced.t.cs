[Introduction]
[Override]
internal class TargetClass
{
  public static global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.Introduced.TargetClass operator +(global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.Introduced.TargetClass x, global::System.Int32 y)
  {
    global::System.Console.WriteLine("Overriding the operator Addition.");
    global::System.Console.WriteLine($"Binary operator Addition({x}, {y})");
    return default(global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.Introduced.TargetClass);
  }
  public static explicit operator global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.Introduced.TargetClass(global::System.Int32 x)
  {
    global::System.Console.WriteLine("Overriding the operator ExplicitConversion.");
    global::System.Console.WriteLine($"Conversion operator ExplicitConversion({x})");
    return default(global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.Introduced.TargetClass);
  }
  public static global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.Introduced.TargetClass operator -(global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.Introduced.TargetClass x)
  {
    global::System.Console.WriteLine("Overriding the operator UnaryNegation.");
    global::System.Console.WriteLine($"Unary operator UnaryNegation({x})");
    return default(global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.Introduced.TargetClass);
  }
}
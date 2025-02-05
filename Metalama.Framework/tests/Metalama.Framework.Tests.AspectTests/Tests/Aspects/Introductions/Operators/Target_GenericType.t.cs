[Introduction]
internal class TargetClass<T>
{
  public static global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Target_GenericType.TargetClass<T> operator +(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Target_GenericType.TargetClass<T> x, global::System.Int32 y)
  {
    global::System.Console.WriteLine($"Unary operator Addition({x}, {y})");
    return default(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Target_GenericType.TargetClass<T>);
  }
  public static explicit operator global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Target_GenericType.TargetClass<T>(global::System.Int32 x)
  {
    global::System.Console.WriteLine($"Unary operator ExplicitConversion({x})");
    return default(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Target_GenericType.TargetClass<T>);
  }
  public static global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Target_GenericType.TargetClass<T> operator -(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Target_GenericType.TargetClass<T> x)
  {
    global::System.Console.WriteLine($"Unary operator UnaryNegation({x})");
    return default(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Target_GenericType.TargetClass<T>);
  }
}
[Introduction]
internal class TargetClass
{
  public static global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Programmatic_CrossAssembly.TargetClass operator +(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Programmatic_CrossAssembly.TargetClass x, global::System.Int32 y)
  {
    global::System.Console.WriteLine($"Unary operator Addition({x}, {y})");
    return default(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Programmatic_CrossAssembly.TargetClass);
  }
  public static explicit operator global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Programmatic_CrossAssembly.TargetClass(global::System.Int32 x)
  {
    global::System.Console.WriteLine($"Unary operator ExplicitConversion({x})");
    return default(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Programmatic_CrossAssembly.TargetClass);
  }
  public static global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Programmatic_CrossAssembly.TargetClass operator -(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Programmatic_CrossAssembly.TargetClass x)
  {
    global::System.Console.WriteLine($"Unary operator UnaryNegation({x})");
    return default(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Programmatic_CrossAssembly.TargetClass);
  }
}
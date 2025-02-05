[Introduction]
internal class TargetClass
{
  public global::System.Int32 IntroducedMethod_DecimalLiteral(global::System.Decimal x = 3.14M)
  {
    global::System.Console.WriteLine($"This is introduced method, x = {x}.");
    return default(global::System.Int32);
  }
  public global::System.Int32 IntroducedMethod_DefaultLiteral(global::System.Int32 x = 0)
  {
    global::System.Console.WriteLine($"This is introduced method, x = {x}.");
    return default(global::System.Int32);
  }
  public global::System.Int32 IntroducedMethod_IntLiteral(global::System.Int32 x = 27)
  {
    global::System.Console.WriteLine($"This is introduced method, x = {x}.");
    return default(global::System.Int32);
  }
  public global::System.Int32 IntroducedMethod_StringLiteral(global::System.String x = "a")
  {
    global::System.Console.WriteLine($"This is introduced method, x = {x}.");
    return default(global::System.Int32);
  }
  public global::System.Int32 IntroducedMethod_StringNullLiteral(global::System.String? x = null)
  {
    global::System.Console.WriteLine($"This is introduced method, x = {x}.");
    return default(global::System.Int32);
  }
}
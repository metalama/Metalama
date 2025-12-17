internal class TargetClass
{
  // Pragma warning before the method signature
#pragma warning disable CA1822
  [Override]
  public void MethodWithPragmaBeforeSignature()
  {
    global::System.Console.WriteLine("Override");
    Console.WriteLine("Original");
    return;
  }
#pragma warning restore CA1822
  // Pragma warning between attribute and method signature
  [Override]
#pragma warning disable CA1822
  public void MethodWithPragmaBetweenAttributeAndSignature()
  {
    global::System.Console.WriteLine("Override");
    Console.WriteLine("Original");
    return;
  }
#pragma warning restore CA1822
  // Pragma warning after parameter list, before body
  [Override]
  public void MethodWithPragmaAfterParameters()
  {
#pragma warning disable CA1822
    global::System.Console.WriteLine("Override");
    Console.WriteLine("Original");
    return;
  }
#pragma warning restore CA1822
  // Pragma warning on return type line
#pragma warning disable CA1822
  [Override]
  public
#pragma warning restore CA1822
  void MethodWithPragmaOnReturnType()
  {
    global::System.Console.WriteLine("Override");
    Console.WriteLine("Original");
    return;
  }
}

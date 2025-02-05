// Final Compilation.Emit failed.
// Error CS0121 on `Method`: `The call is ambiguous between the following methods or properties: 'TargetCode.Method(int)' and 'TargetCode.Method(int)'`
// Error CS0111 on `Method`: `Type 'TargetCode' already defines a member called 'Method' with the same parameter types`
internal class TargetCode
{
  [Aspect]
  private int Method(int a)
  {
    global::System.Console.WriteLine("Aspect");
    return this.Method(a);
  }
  int Method(int a)
  {
    return a;
  }
}
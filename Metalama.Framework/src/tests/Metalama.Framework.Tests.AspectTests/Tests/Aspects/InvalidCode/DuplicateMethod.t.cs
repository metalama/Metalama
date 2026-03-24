// Final Compilation.Emit failed.
// Error CS0111 on `Method`: `Type 'TargetCode' already defines a member called 'Method' with the same parameter types`
internal class TargetCode
{
  [Aspect]
  private int Method(int a)
  {
    global::System.Console.WriteLine("Aspect");
    return a;
  }
  int Method(int a)
  {
    return a;
  }
}
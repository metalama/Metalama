// Final Compilation.Emit failed.
// Error CS0103 on `Foo`: `The name 'Foo' does not exist in the current context`
internal class TargetCode
{
  [Aspect]
  private void Method(int a)
  {
    global::System.Console.WriteLine("Aspect");
    Foo.Bar();
    return;
  }
}
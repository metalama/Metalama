// Final Compilation.Emit failed.
// Error CS0111 on `Foo`: `Type 'TargetCode' already defines a member called 'Foo' with the same parameter types`
// Error CS0121 on `Foo`: `The call is ambiguous between the following methods or properties: 'TargetCode.Foo()' and 'TargetCode.Foo()'`
internal class TargetCode
{
  [Aspect]
  public int Foo()
  {
    return 42;
  }
  [Aspect]
  public int Foo()
  {
    return this.Foo();
  }
}
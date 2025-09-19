// Final Compilation.Emit failed.
// Error CS0121 on `Foo`: `The call is ambiguous between the following methods or properties: 'TargetCode.Foo()' and 'TargetCode.Foo()'`
// Error CS0111 on `Foo`: `Type 'TargetCode' already defines a member called 'Foo' with the same parameter types`
internal class TargetCode
{
  [Aspect]
  public int Foo()
  {
    return this.Foo();
  }
  [Aspect]
  public int Foo()
  {
    return 42;
  }
}
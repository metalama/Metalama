// Warning TEST0002 on `F`: `Reference to 'F' from 'Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.SharedIndex.DeclarationRoots_TypeScope.C.Value' (Invocation), index restricted to declaration roots: True.`
// Warning TEST0002 on `F`: `Reference to 'F' from 'Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.SharedIndex.DeclarationRoots_TypeScope.C.M()' (Invocation), index restricted to declaration roots: True.`
[TheAspect]
internal class C
{
  public int Value = A.F();
  private void M() => A.F();
}
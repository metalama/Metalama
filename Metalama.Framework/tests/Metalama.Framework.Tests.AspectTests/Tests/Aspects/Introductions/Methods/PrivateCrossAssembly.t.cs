// Final Compilation.Emit failed.
// Error CS0122 on `InternalClass`: `'InternalClass' is inaccessible due to its protection level`
// Error CS0122 on `InternalClass`: `'InternalClass' is inaccessible due to its protection level`
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.PrivateCrossAssembly;
[MyAspect]
internal class C
{
  private global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.PrivateCrossAssembly.InternalClass IntroducedMethod()
  {
    return (global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.PrivateCrossAssembly.InternalClass)(new());
  }
}
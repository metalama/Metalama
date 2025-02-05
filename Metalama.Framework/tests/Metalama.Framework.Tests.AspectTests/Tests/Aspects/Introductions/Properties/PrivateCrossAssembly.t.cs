// Final Compilation.Emit failed.
// Error CS0122 on `InternalClass`: `'InternalClass' is inaccessible due to its protection level`
// Error CS0122 on `InternalClass`: `'InternalClass' is inaccessible due to its protection level`
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Properties.PrivateCrossAssembly;
[MyAspect]
internal class C
{
  private global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Properties.PrivateCrossAssembly.InternalClass Introduced { get; } = (global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Properties.PrivateCrossAssembly.InternalClass)(new());
}
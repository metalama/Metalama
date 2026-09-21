using Metalama.Framework.Aspects;
using System;
#pragma warning disable CS0067
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.FileLocalType_Introduce;
// A member introduced into a file-local type has no symbol of its own, so its identifier takes the discriminator from
// the type that declares it. See issue #662. This test guards the transformation itself, which the compile-time
// pipeline performs without the identifier; the identifier is what the design-time pipeline requires.
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class IntroduceMembersAttribute : TypeAspect
{
  [Introduce]
  public int IntroducedMethod(int a) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  [Introduce]
  public int IntroducedProperty { get; set; }
  [Introduce]
  public event EventHandler? IntroducedEvent;
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
[IntroduceMembers]
file class FileLocalTarget
{
  public int Existing { get; set; }
  public global::System.Int32 IntroducedProperty { get; set; }
  public global::System.Int32 IntroducedMethod(global::System.Int32 a)
  {
    return a;
  }
  public event global::System.EventHandler? IntroducedEvent;
}
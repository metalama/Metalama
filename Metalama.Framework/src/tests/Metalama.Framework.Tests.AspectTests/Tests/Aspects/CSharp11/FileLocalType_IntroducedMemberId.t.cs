using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.FileLocalType_IntroducedMemberId;
// A member introduced into a source file-local type has no symbol of its own, so its serializable identifier takes the
// discriminator from the type that declares it. See issue #662.
//
// The discriminator is the metadata name that the compiler gives the file-local type. It embeds the name of the
// declaring file and a checksum of its path, and both differ between machines, so this test reports the shape of the
// identifier rather than the identifier itself.
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class IntroduceAndReportAttribute : TypeAspect
{
  [Template]
  public int IntroducedMethod(int a) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  [Template]
  [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
  private static string ReportId([CompileTime] string id) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  public override void BuildAspect(IAspectBuilder<INamedType> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  /// <summary>
  /// Replaces the value of the file-local discriminator with a constant, so that the result does not depend on the
  /// path of the declaring file.
  /// </summary>
  private static string MaskDiscriminator(string id)
  {
    const string marker = ";File=<";
    var index = id.IndexOf(marker, StringComparison.Ordinal);
    if (index < 0)
    {
      return id + " (no file discriminator)";
    }
    var endOfDiscriminator = id.IndexOf("__", index, StringComparison.Ordinal);
    if (endOfDiscriminator < 0)
    {
      return id + " (malformed file discriminator)";
    }
    return id.Substring(0, index) + ";File=<masked>" + id.Substring(endOfDiscriminator);
  }
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
[IntroduceAndReport]
file class FileLocalTarget
{
  private static global::System.String GetIntroducedMemberIdShape()
  {
    return "M:Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.FileLocalType_IntroducedMemberId.FileLocalTarget.IntroducedMethod(System.Int32)~System.Int32;File=<masked>__FileLocalTarget";
  }
  public global::System.Int32 IntroducedMethod(global::System.Int32 a)
  {
    return a;
  }
}
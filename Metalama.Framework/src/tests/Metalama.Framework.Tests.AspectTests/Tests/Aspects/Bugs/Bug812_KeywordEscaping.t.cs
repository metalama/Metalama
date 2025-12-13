using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug812_KeywordEscaping;
// Issue #812: Names of introduced declarations should be @ escaped when necessary
// When introducing a field with a name that is a C# keyword, the generated code
// should escape the name with @ (e.g., @const), but currently it doesn't.
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class IntroduceKeywordFieldAspect : TypeAspect
{
  public override void BuildAspect(IAspectBuilder<INamedType> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
[IntroduceKeywordFieldAspect]
internal class TargetClass
{
  private global::System.Int32 @const;
}
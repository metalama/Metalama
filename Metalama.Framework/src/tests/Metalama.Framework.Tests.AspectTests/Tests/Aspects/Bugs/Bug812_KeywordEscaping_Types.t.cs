using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug812_KeywordEscaping_Types;
// Issue #812: Tests introduction of nested types with C# keyword names
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class IntroduceKeywordTypesAspect : TypeAspect
{
  public override void BuildAspect(IAspectBuilder<INamedType> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
[IntroduceKeywordTypesAspect]
internal class TargetClass
{
  class @class
  {
  }
  class @struct
  {
  }
}
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug812_KeywordEscaping_AllMembers;
// Issue #812: Tests introduction of all member kinds with C# keyword names
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class IntroduceAllKeywordMembersAspect : TypeAspect
{
  public override void BuildAspect(IAspectBuilder<INamedType> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  [Template]
  public int FieldTemplate;
  [Template]
  public string PropertyTemplate { get; set; }
  [Template]
  public void MethodTemplate() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  [Template]
  public void MethodWithKeywordParamTemplate(string @class) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  [Template]
  public event Action EventTemplate;
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
[IntroduceAllKeywordMembersAspect]
internal class TargetClass
{
  public global::System.Int32 @const;
  public global::System.String @int { get; set; }
  public void MethodWithKeywordParam(global::System.String @class)
  {
  }
  public void @void()
  {
  }
  public event global::System.Action @event;
}
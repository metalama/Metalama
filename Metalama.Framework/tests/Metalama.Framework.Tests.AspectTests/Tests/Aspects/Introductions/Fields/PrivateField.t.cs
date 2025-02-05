using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;
namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Fields.PrivateField;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class IntroducePrivateFieldAttribute : OverrideMethodAspect
{
  [Introduce]
  [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
  private readonly string _text;
  public override dynamic? OverrideMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
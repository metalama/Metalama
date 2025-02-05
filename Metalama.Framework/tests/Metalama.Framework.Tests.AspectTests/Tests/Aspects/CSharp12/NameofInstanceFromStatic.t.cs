using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.NameofInstanceFromStatic;
#pragma warning disable CS0649 // Field is never assigned
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class TheAspect : MethodAspect
{
  public override void BuildAspect(IAspectBuilder<IMethod> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  private string? p;
  [Template]
  [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
  private static string M() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class C
{
  private string? p;
  [TheAspect]
  private static string M()
  {
    return (global::System.String)(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.NameofInstanceFromStatic.C.M_Source() + "Length");
  }
  private static string M_Source() => nameof(p.Length);
}
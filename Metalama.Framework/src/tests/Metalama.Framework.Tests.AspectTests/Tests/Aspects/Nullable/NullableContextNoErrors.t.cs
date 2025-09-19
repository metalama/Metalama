using Metalama.Framework.Aspects;
namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Nullable.NullableContextNoErrors
{
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  internal class Aspect : TypeAspect
  {
    [Introduce]
    [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
    private string Introduced1(string a) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
#nullable enable
    [Introduce]
    [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
    private string Introduced2(string? a) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
    [Introduce]
    [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
    private string Introduced3(int x) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
}
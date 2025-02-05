using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.RefReadonlyParameter_Invoke;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class TheAspect : TypeAspect
{
  public override void BuildAspect(IAspectBuilder<INamedType> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  [Template]
  [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
  private void Called(in int i, ref readonly int j) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  [Template]
  [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
  private void Caller(IMethod called, in int i, ref int j, ref readonly int k) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
[TheAspect]
internal class C
{
  private void Called(in global::System.Int32 i, ref readonly global::System.Int32 j)
  {
  }
  private void Caller(in global::System.Int32 i, ref global::System.Int32 j, ref readonly global::System.Int32 k)
  {
    this.Called(i, in i);
    this.Called(j, in j);
    this.Called(k, in k);
  }
}
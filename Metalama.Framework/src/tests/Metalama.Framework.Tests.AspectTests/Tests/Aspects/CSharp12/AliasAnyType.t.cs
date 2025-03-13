using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using MyTuple = (int, int Name);
using unsafe IntPointer = int*;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.AliasAnyType;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class TheAspect : MethodAspect
{
  public override void BuildAspect(IAspectBuilder<IMethod> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  [CompileTime]
  private void CompileTimeMethod(MyTuple tuple) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  [Template]
  [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
  private static void M(MyTuple tuple) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  [Introduce]
  [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
  private static void Introduced(MyTuple tuple) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class C
{
  [TheAspect]
  private static unsafe void M(MyTuple tuple, IntPointer ptr)
  {
  }
  private static void Introduced((global::System.Int32, global::System.Int32 Name) tuple)
  {
  }
}
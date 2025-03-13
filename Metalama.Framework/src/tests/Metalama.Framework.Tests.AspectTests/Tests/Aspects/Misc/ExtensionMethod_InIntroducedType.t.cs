using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Diagnostics;
[assembly: Metalama.Framework.Tests.Integration.Tests.Aspects.Misc.ExtensionMethod_InIntroducedType.TheAspect]
namespace Metalama.Framework.Tests.Integration.Tests.Aspects.Misc.ExtensionMethod_InIntroducedType;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
class TheAspect : CompilationAspect
{
  [Template]
  [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
  static void ExtensionMethodTemplate([This] object self) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  [Template]
  [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
  static void Usage() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  public override void BuildAspect(IAspectBuilder<ICompilation> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
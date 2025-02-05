using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.TestFramework.Html.TopLevelClassIntroduction;
[assembly: IntroduceTopLevelClass]
namespace Metalama.Framework.IntegrationTests.TestFramework.Html.TopLevelClassIntroduction
{
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  public class IntroduceTopLevelClassAttribute : CompilationAspect
  {
    public override void BuildAspect(IAspectBuilder<ICompilation> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
}
using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.ScopeTarget;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class TheAspect : Attribute, IAspect<IDeclaration>
{
  public void BuildAspect(IAspectBuilder<IDeclaration> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  public void BuildEligibility(IEligibilityBuilder<IDeclaration> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  [Introduce(Scope = IntroductionScope.Target)]
  public void IntroducedMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
[TheAspect]
public class InstanceClass
{
  public void IntroducedMethod()
  {
  }
}
[TheAspect]
public static class StaticClass
{
  public static void IntroducedMethod()
  {
  }
}
public class Class1
{
  [TheAspect]
  public void InstanceMember()
  {
  }
  public void IntroducedMethod()
  {
  }
}
public class Class3
{
  // The class is intentionally not static.
  [TheAspect]
  public static void StaticMember()
  {
  }
  public static void IntroducedMethod()
  {
  }
}
using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.OverrideOverriddenMethodWithGenericConstraints;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class MyAspect : OverrideMethodAspect
{
  public override dynamic? OverrideMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class BaseClass
{
  public virtual void M<T>(T t)
    where T : IDisposable
  {
    t.Dispose();
  }
}
public class DerivedClass : BaseClass
{
  [MyAspect]
  // Generic parameters must not be repeated.
  public override void M<T>(T t)
  {
    global::System.Console.WriteLine("Override");
    this.M_Source<T>(t);
    this.M_Source<T>(t);
    return;
  }
  private void M_Source<T>(T t)
    where T : global::System.IDisposable
  {
    t.Dispose();
  }
}
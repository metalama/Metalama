using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Inheritance.ManyDerivedTypes
{
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  [Inheritable]
  internal class Aspect : TypeAspect
  {
    public override void BuildAspect(IAspectBuilder<INamedType> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
    [Template]
    [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
    private dynamic? Template() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  [Aspect]
  public interface IBase
  {
  }
  public interface ISub1 : IBase
  {
  }
  public interface ISub2 : IBase
  {
  }
  public interface IDerived : ISub1, ISub2
  {
  }
  public class BaseClass : IDerived
  {
    private void M()
    {
      global::System.Console.WriteLine("Overridden!");
      return;
    }
  }
  public class DerivedClass : BaseClass
  {
    private void N()
    {
      global::System.Console.WriteLine("Overridden!");
      return;
    }
  }
}
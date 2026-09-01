using Metalama.Framework.Aspects;
using System.Linq;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.Record_Implicit;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.Record_Implicit
{
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  internal class MyAspect : OverrideFieldOrPropertyAspect
  {
    public override dynamic? OverrideProperty { get => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time."); set => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time."); }
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  [ApplyAspect]
  internal record MyRecord(int A, int B)
  {
    protected virtual global::System.Type EqualityContract
    {
      get
      {
        return typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.Record_Implicit.MyRecord);
      }
    }
  }
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  internal class ApplyAspect : TypeAspect
  {
    public override void BuildAspect(IAspectBuilder<INamedType> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
}
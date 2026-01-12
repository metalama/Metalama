using Metalama.Framework.Aspects;
using System;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Issue1278_InitializerWithLambda;
[CompileTime]
public enum Permission
{
  NotSet,
  Read,
  Write
}
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class TheAspect : TypeAspect
{
  public static Permission RequestedPermission { get; set; } = Permission.Read;
  [Introduce]
  public Func<int> IntroducedProperty { get; }
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
[TheAspect]
internal class TargetCode
{
  public global::System.Func<global::System.Int32> IntroducedProperty { get; } = (global::System.Func<global::System.Int32>)(() =>
  {
    return (global::System.Int32)2;
  });
}
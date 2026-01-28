using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_Simple;
// Test override WITHOUT field keyword to verify template expansion works
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class LoggingAspect : TypeAspect
{
  public override void BuildAspect(IAspectBuilder<INamedType> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  [Template]
  public string? PropertyTemplate { get => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time."); set => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time."); }
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
[LoggingAspect]
internal class C
{
  private string? _name;
  public string? Name
  {
    get
    {
      global::System.Console.WriteLine("Getting");
      return this._name;
    }
    set
    {
      global::System.Console.WriteLine($"Setting to {value}");
      this._name = value;
    }
  }
}

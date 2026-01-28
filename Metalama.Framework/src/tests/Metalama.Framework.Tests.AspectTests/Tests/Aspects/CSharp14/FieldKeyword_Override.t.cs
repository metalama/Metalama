using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class LoggingAspect : TypeAspect
{
  public override void BuildAspect(IAspectBuilder<INamedType> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  [Template]
  public string? PropertyTemplate {[global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Public, IsAsync = false, IsIteratorMethod = false, IntroducesBackingField = true)]
    get => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time."); [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Public, IsAsync = false, IsIteratorMethod = false, IntroducesBackingField = true)]
    set => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time."); }
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
[LoggingAspect]
internal class C
{
  public string? Name
  {
    get
    {
      return (global::System.String? )_name;
    }
    set
    {
      global::System.Console.WriteLine($"Setting to {value}");
      _name = value;
    }
  }
  private global::System.String? _name;
}

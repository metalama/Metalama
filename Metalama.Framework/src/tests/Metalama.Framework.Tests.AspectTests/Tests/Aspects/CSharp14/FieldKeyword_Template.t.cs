using Metalama.Framework.Aspects;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Template;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class TheAspect : TypeAspect
{
  [Introduce]
  public string? Property {[global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Public, IsAsync = false, IsIteratorMethod = false, IntroducesBackingField = true)]
    get => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time."); [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Public, IsAsync = false, IsIteratorMethod = false, IntroducesBackingField = true)]
    set => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time."); }
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
[TheAspect]
internal class C
{
  private global::System.String? _property;
  public global::System.String? Property
  {
    get
    {
      return (global::System.String? )_property;
    }
    set
    {
      _property = value;
    }
  }
}

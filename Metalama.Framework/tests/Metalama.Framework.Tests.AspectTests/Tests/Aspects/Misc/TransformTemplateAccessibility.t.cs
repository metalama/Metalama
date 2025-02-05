using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
#pragma warning disable CS0067, CS0414
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.TransformTemplateAccessibility
{
  /*
    Tests that the visibility of all templates, including accessors, is not changed, and
    that an [Accessibility] attribute is added.
     */
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  internal class MyAspect : TypeAspect
  {
    public override void BuildAspect(IAspectBuilder<INamedType> builder) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
    [Template]
    [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
    private void MethodTemplate() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
    [Template]
    [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Internal, IsAsync = false, IsIteratorMethod = false)]
    internal int AutomaticPropertyTemplate {[global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Internal, IsAsync = false, IsIteratorMethod = false)]
      get; [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
      private set; }
    [Template]
    [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Internal, IsAsync = false, IsIteratorMethod = false)]
    internal int ExplicitPropertyTemplate {[global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Internal, IsAsync = false, IsIteratorMethod = false)]
      get => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time."); [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
      private set => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time."); }
    [Template]
    [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
    private int _fieldTemplate;
    [Template]
    [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Internal, IsAsync = false, IsIteratorMethod = false)]
    internal event EventHandler? EventTemplate;
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
}
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Fields.PrivateFieldAccessibility;
#pragma warning disable CS0414 // The field 'IntroducePrivateFieldAttribute._field' is assigned but its value is never used
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class IntroducePrivateFieldAttribute : IAspect
{
  [Introduce]
  [global::Metalama.Framework.Aspects.CompiledTemplateAttribute(Accessibility = global::Metalama.Framework.Code.Accessibility.Private, IsAsync = false, IsIteratorMethod = false)]
  private readonly RunTimeOnlyClass _field;
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class RunTimeOnlyClass
{
}
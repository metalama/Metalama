[Aspect]
public class Target
{
  private void FromBaseCompilation()
  {
    global::System.Console.WriteLine(typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameter.IntroducedType.Target.IntroducedType).Name);
  }
  private void FromMutableCompilation()
  {
    global::System.Console.WriteLine(typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameter.IntroducedType.Target.IntroducedType).Name);
  }
  class IntroducedType
  {
  }
}
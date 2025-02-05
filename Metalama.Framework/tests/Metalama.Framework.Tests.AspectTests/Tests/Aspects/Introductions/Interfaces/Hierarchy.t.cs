[IntroductionAttribute]
public class TargetType
{
  public interface ISourceBase;
  interface IIntroducedBase
  {
  }
  interface ITest : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.Hierarchy.TargetType.ISourceBase, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.Hierarchy.TargetType.IIntroducedBase
  {
  }
}
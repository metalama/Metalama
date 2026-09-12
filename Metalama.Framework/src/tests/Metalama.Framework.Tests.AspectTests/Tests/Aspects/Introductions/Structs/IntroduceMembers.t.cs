[Introduction]
public class TargetType
{
  public readonly struct ReadOnlyStruct : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Structs.IntroduceMembers.IHasLabel
  {
    public ReadOnlyStruct()
    {
    }
    public global::System.String Label { get; init; }
    public global::System.Int32 MethodTemplate()
    {
      return (global::System.Int32)42;
    }
  }
}
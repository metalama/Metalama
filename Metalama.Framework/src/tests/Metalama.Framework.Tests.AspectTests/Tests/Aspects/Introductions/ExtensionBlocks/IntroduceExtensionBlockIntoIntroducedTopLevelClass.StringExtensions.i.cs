namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.ExtensionBlocks.IntroduceExtensionBlockIntoIntroducedTopLevelClass
{
  static class StringExtensions
  {
    extension(global::System.String self)
    {
      public global::System.Int32 GetLength()
      {
        return (global::System.Int32)42;
      }
    }
  }
}

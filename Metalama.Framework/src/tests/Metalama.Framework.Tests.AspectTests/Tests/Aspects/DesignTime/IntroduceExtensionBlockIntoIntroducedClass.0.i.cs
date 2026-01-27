namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceExtensionBlockIntoIntroducedClass
{
  static partial class StringExtensions
  {
    extension(global::System.String self)
    {
      public global::System.Int32 GetDoubleLength()
      {
        return default(global::System.Int32);
      }
    }
  }
}

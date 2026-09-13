namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.MembersAtDesignTime
{
  partial class TargetType
  {
    public partial record Positional(global::System.Int32 Value)
    {
      public global::System.Int32 MethodTemplate()
      {
        return default(global::System.Int32);
      }
    }
  }
}
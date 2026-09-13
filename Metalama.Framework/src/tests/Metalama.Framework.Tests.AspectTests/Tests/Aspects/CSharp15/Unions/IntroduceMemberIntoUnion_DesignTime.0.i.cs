namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.Unions.IntroduceMemberIntoUnion_DesignTime
{
  partial class TargetType
  {
    public partial union Result(global::System.Int32, global::System.String)
    {
      public global::System.Int32 MethodTemplate()
      {
        return default(global::System.Int32);
      }
    }
  }
}
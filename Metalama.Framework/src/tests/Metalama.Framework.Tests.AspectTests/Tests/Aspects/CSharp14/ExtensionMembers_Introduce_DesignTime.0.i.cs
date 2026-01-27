namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_Introduce_DesignTime
{
  static partial class C
  {
    extension(global::System.String)
    {
      public static global::System.Int32 SomeStaticMethod(global::System.Int32 a, global::System.Int32 b)
      {
        return default(global::System.Int32);
      }
    }
  }
}

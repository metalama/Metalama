namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_Introduce_DesignTime
{
  partial class C
  {
    extension(global::System.Int32 test)
    {
      public global::System.Int32 SomeMethod(global::System.Int32 a, global::System.Int32 b)
      {
        return default(global::System.Int32);
      }
      public static global::System.Collections.Generic.IEnumerable<global::System.Int32> operator +(global::System.Collections.Generic.IEnumerable<global::System.Int32> a, global::System.Int32 b) => default(global::System.Collections.Generic.IEnumerable<global::System.Int32>);
    }
  }
}
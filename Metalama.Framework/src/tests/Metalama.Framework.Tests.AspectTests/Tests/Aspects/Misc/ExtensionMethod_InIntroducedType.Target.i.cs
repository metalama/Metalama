namespace Metalama.Framework.Tests.Integration.Tests.Aspects.Misc.ExtensionMethod_InIntroducedType
{
  static class Target
  {
    private static void ExtensionMethodTemplate(this global::System.Object self)
    {
    }
    private static void Usage()
    {
      var o = new object ();
      o.ExtensionMethodTemplate();
    }
  }
}
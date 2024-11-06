[TheAspect]
static class Target
{
  private static void ExtensionMethodTemplate(this global::System.Object self)
  {
  }
  private static void RegularTemplate(this in global::System.Int32 o)
  {
  }
  private static void Usage()
  {
    var o = new object ();
    o.ExtensionMethodTemplate();
    var i = 42;
    i.RegularTemplate();
  }
}
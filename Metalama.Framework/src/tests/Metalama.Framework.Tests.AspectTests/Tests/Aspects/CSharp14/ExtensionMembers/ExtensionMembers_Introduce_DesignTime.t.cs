[TheAspect]
internal static partial class C
{
  extension(int test)
  {
    public int ExtensionProperty
    {
      get => 5;
      set
      {
      }
    }
  }
  extension(string)
  {
    public static int StaticExtensionProperty
    {
      get => 5;
      set
      {
      }
    }
  }
}
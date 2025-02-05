[assembly: AddTag("Tagged")]
namespace Metalama.Extensions.Multicast.AspectTests.AddToAllFromCompilation
{
  [Tag("Tagged")]
  public class AClass
  {
    [Tag("Tagged")]
    [return: Tag("Tagged")]
    public int Method([Tag("Tagged")] int p) => 0;
    [Tag("Tagged")]
    public int Property {[Tag("Tagged")]
      [return: Tag("Tagged")]
      get; [Tag("Tagged")]
      private set; }
    [Tag("Tagged")]
    public int Field;
    [Tag("Tagged")]
    public event Action PublicEvent;
  }
  [Tag("Tagged")]
  public class AStruct
  {
    [Tag("Tagged")]
    private class Nested
    {
      [Tag("Tagged")]
      [return: Tag("Tagged")]
      public int Method([Tag("Tagged")] int p) => 0;
      [Tag("Tagged")]
      public int Property {[Tag("Tagged")]
        [return: Tag("Tagged")]
        get; [Tag("Tagged")]
        private set; }
      [Tag("Tagged")]
      public int Field;
      [Tag("Tagged")]
      public event Action PublicEvent;
    }
  }
  [Tag("Tagged")]
  public enum AnEnum
  {
  }
  [Tag("Tagged")]
  public delegate void ADelegate();
}
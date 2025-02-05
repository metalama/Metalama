public class AClass
{
  [AddTag("Tagged")]
  [Tag("Tagged")]
  [return: Tag("Tagged")]
  public int Method([Tag("Tagged")] int p) => 0;
  [AddTag("Tagged")]
  [Tag("Tagged")]
  public int Property {[Tag("Tagged")]
    [return: Tag("Tagged")]
    get; [Tag("Tagged")]
    private set; }
  public int Field;
  [AddTag("Tagged")]
  [Tag("Tagged")]
  public event Action PublicEvent
  {
    [Tag("Tagged")]
    add
    {
    }
    [Tag("Tagged")]
    remove
    {
    }
  }
}
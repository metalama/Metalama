[AddTag("Tagged")]
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
  public event Action PublicEvent;
  // Nested classes are ignored for PostSharp compatibility.
  private class Nested
  {
    private void Method()
    {
    }
  }
}
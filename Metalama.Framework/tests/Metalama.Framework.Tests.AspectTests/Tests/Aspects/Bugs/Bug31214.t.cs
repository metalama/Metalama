public class Foo
{
  [MyAspect("The title")]
  private void M()
  {
    var httpResult = "N/A";
    global::System.Collections.Generic.Dictionary<global::System.String, global::System.String> result = new()
    {
      {
        "Title",
        "The title"
      },
      {
        "HTTP result",
        httpResult
      }
    };
    return;
  }
}
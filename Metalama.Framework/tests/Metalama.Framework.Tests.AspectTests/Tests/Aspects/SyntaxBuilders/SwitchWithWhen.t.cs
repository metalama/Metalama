[TheAspect]
internal class C
{
  private string? A { get; set; }
  private string? B { get; set; }
  public void TheMethod(global::System.String propertyName, global::System.String otherName)
  {
    switch (propertyName)
    {
      case "A"when otherName == "xxx":
        global::System.Console.WriteLine("A");
        break;
      case "B"when otherName == "xxx":
        global::System.Console.WriteLine("B");
        break;
      default:
        return;
    }
  }
}
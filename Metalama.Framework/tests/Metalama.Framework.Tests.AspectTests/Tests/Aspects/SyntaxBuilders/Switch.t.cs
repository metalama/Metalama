[TheAspect]
internal class C
{
  private string? A { get; set; }
  private string? B { get; set; }
  public void TheMethod(global::System.String propertyName)
  {
    switch (propertyName)
    {
      case "A":
        global::System.Console.WriteLine("A");
        break;
      case "B":
        global::System.Console.WriteLine("B");
        break;
      default:
        return;
    }
  }
}
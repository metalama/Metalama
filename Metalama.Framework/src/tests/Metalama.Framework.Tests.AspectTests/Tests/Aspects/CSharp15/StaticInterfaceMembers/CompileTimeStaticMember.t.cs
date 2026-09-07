internal class TargetType
{
  [TheAspect]
  public void Method()
  {
    global::System.Console.WriteLine("[42]");
    return;
  }
}
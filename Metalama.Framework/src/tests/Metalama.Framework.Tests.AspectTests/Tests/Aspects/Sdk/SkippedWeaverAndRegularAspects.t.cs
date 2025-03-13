internal class TargetCode
{
  [RegularAspect1]
  [RegularAspect2]
  private void M()
  {
    global::System.Console.WriteLine("Added by regular aspect #1.");
    global::System.Console.WriteLine("Added by regular aspect #2.");
    return;
  }
}
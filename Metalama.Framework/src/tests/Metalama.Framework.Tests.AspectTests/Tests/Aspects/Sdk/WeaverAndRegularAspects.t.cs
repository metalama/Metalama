internal class TargetCode
{
  [RegularAspect1]
  [RegularAspect2]
  [WeaverAspect]
  private void M()
  {
    global::System.Console.WriteLine("Added by regular aspect #1.");
    Console.WriteLine("Added by weaver.");
    global::System.Console.WriteLine("Added by regular aspect #2.");
    return;
  }
}
[TopLevelAspect]
internal class C
{
  public void EligibleMethod()
  {
    global::System.Console.WriteLine("Overridden.");
    return;
  }
  public static void NonEligibleMethod()
  {
  }
}
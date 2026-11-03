internal class Target
{
  [Aspect]
  private static void UnreachableAfterReturn()
  {
    return;
    throw new Exception();
    return;
    throw new global::System.NotImplementedException();
  }
  [Aspect]
  private static void ReachableAfterReturn(int i)
  {
    if (i == 0)
    {
      goto label;
    }
    return;
    label:
      Console.WriteLine("Test");
    return;
    throw new global::System.NotImplementedException();
  }
}
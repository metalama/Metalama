internal class Target
{
  [Aspect]
  private static void UnreachableAfterReturn()
  {
    goto __aspect_return_1;
    throw new Exception();
    __aspect_return_1:
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
    goto __aspect_return_1;
    label:
      Console.WriteLine("Test");
    __aspect_return_1:
      return;
    throw new global::System.NotImplementedException();
  }
}
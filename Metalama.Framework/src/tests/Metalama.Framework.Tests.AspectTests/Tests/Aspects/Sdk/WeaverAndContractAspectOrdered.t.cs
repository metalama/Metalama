[MakeVirtual]
internal class TargetCode
{
  public string Process([NotNull] string input)
  {
    if (input == null)
    {
      throw new global::System.ArgumentNullException("input");
    }
    global::System.Console.WriteLine("Added by weaver.");
    return input;
  }
}
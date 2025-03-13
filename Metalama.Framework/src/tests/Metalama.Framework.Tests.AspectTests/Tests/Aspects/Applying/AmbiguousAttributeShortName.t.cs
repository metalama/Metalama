internal class TargetClass
{
  [RequiresAttribute]
  private void ShortName()
  {
    global::System.Console.WriteLine("Applied.");
    return;
  }
  [RequiresAttributeAttribute]
  private void LongName()
  {
    global::System.Console.WriteLine("Applied.");
    return;
  }
}
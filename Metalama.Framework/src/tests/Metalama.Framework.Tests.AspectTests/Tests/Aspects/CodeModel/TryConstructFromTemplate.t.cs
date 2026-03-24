internal class Target
{
  [TestAspect]
  [MyCustomAttribute("Hello")]
  private void M()
  {
    global::System.Console.WriteLine("TryConstruct: Hello");
    global::System.Console.WriteLine("Construct: Hello");
    return;
  }
}

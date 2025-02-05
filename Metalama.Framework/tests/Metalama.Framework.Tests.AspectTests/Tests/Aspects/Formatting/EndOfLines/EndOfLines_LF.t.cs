// ERROR: Expected "\n" end of lines, but got "\r\n" at position 1299.
public class Target
{
  [TestAspect1]
  [TestAspect2]
  private static int Add(int a, int b)
  {
    Console.WriteLine("Hello!");
    Console.WriteLine("Hello!");
    Console.WriteLine("Thinking...");
    return a + b;
  }
}
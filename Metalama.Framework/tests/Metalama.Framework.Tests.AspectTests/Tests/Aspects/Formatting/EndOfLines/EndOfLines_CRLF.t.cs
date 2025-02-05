// ERROR: Expected "\r\n" end of lines, but got "\n" at position 307.
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
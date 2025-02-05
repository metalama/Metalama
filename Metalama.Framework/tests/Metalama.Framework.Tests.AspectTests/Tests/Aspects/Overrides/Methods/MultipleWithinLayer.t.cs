internal class TargetClass
{
  [Override]
  public void TargetMethod()
  {
    global::System.Console.WriteLine("This is the overriding layer 'second', order 3.");
    global::System.Console.WriteLine("This is the overriding layer 'second', order 2.");
    global::System.Console.WriteLine("This is the overriding layer 'second', order 1.");
    global::System.Console.WriteLine("This is the overriding layer 'first', order 3.");
    global::System.Console.WriteLine("This is the overriding layer 'first', order 2.");
    global::System.Console.WriteLine("This is the overriding layer 'first', order 1.");
    global::System.Console.WriteLine("This is the overriding layer 'default', order 3.");
    global::System.Console.WriteLine("This is the overriding layer 'default', order 2.");
    global::System.Console.WriteLine("This is the overriding layer 'default', order 1.");
    Console.WriteLine("This is the original method.");
    return;
  }
}
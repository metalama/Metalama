file class FileLocalTarget
{
  [Log]
  public int Add(int a, int b)
  {
    global::System.Console.WriteLine("Add started.");
    return a + b;
  }
  [Log]
  public T Echo<T>(T value)
  {
    global::System.Console.WriteLine("Echo started.");
    return value;
  }
}
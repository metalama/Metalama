private int Method(int a)
{
  lock (this)
  {
    global::System.Console.WriteLine(1);
    return this.Method(a);
  }
}
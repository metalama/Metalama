private int Method(int a, int bb)
{
  if (this.logMembers)
  {
    global::System.Console.WriteLine($"logMembers = {this.logMembers}");
  }
  return this.Method(a, bb);
}
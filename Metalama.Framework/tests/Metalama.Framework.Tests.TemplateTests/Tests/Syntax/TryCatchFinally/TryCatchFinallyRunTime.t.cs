private int Method(int a)
{
  try
  {
    global::System.Console.WriteLine("try");
    var result = this.Method(a);
    global::System.Console.WriteLine("success");
    return (global::System.Int32)result;
  }
  catch
  {
    global::System.Console.WriteLine("exception 0");
    throw;
  }
  finally
  {
    global::System.Console.WriteLine("finally");
  }
  global::System.Console.WriteLine(0);
}
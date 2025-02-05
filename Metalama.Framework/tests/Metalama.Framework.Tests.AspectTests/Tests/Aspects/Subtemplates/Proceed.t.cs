[Aspect]
private async Task Method(bool condition)
{
  await global::System.Threading.Tasks.Task.Yield();
  global::System.Console.WriteLine("regular template");
  if (condition)
  {
    global::System.Console.WriteLine("static template i=1");
    await this.Method_Source(condition);
    return;
  }
  else
  {
    global::System.Console.WriteLine("static template i=2");
    await this.Method_Source(condition);
    return;
  }
  throw new global::System.Exception();
}
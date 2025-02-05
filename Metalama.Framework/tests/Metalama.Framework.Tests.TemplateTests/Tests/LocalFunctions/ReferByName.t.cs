private int Method(int a)
{
  void TheLocalFunction(object? state)
  {
    this.Method(a);
  }
  global::System.Threading.ThreadPool.QueueUserWorkItem(TheLocalFunction);
}
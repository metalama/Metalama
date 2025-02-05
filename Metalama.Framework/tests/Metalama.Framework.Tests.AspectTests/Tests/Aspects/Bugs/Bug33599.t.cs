internal class Target
{
  [Test1]
  public MemoryStream M1()
  {
    _ = (global::System.IO.MemoryStream)new MemoryStream();
    return default;
  }
  [Test2]
  public MemoryStream M2()
  {
    _ = (global::System.IO.MemoryStream)new MemoryStream();
    return default;
  }
  [Test1]
  public MemoryStream M3()
  {
    _ = (global::System.IO.MemoryStream)(new());
    return default;
  }
  [Test2]
  public MemoryStream M4()
  {
    _ = (global::System.IO.MemoryStream)(new());
    return default;
  }
}
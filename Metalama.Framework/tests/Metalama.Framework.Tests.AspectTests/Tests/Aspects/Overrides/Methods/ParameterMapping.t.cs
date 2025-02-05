[Introduction]
internal class TargetClass
{
  public int Method_InvertedParameters(string x, int y)
  {
    global::System.Int32 z;
    z = x.Length + y;
    return (global::System.Int32)(x.Length + y);
  }
  public int Method_SelectFirstParameter(string x, int y)
  {
    global::System.Int32 z;
    z = x.Length + y;
    return (global::System.Int32)x.Length;
  }
  public int Method_SelectSecondParameter(string x, int y)
  {
    global::System.Int32 z;
    z = x.Length + y;
    return y;
  }
}
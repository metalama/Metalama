internal class TargetCode
{
  [AddParameter]
  private TargetCode(string s, global::System.Int32 arg = default(global::System.Int32))
  {
  }
  private TargetCode(int i) : this(i.ToString(), 42)
  {
  }
}
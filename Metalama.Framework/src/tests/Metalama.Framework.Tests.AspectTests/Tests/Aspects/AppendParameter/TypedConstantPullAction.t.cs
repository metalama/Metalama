internal class TargetCode
{
  [AddParameter]
  private TargetCode(string s, int arg = default)
  {
  }
  private TargetCode(int i) : this(i.ToString(), 42)
  {
  }
}
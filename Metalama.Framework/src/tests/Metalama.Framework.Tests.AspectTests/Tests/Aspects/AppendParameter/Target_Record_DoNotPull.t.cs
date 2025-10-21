public record R
{
  [MyAspect]
  public R([global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Int32 p = 15)
  {
  }
  public R(string s) : this()
  {
  }
}
public record S : R
{
  public S()
  {
  }
}
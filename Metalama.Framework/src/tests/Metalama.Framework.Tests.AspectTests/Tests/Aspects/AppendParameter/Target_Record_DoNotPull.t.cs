public record R
{
  [MyAspect]
  public R([AspectGenerated] int p = 15)
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
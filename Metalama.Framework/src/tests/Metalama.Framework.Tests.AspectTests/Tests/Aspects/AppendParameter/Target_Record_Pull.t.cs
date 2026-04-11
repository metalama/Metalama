public record R
{
  [MyAspect]
  public R([AspectGenerated] int p = 15)
  {
  }
  public R(string s) : this(51)
  {
  }
}
public record S1 : R
{
  public S1() : base(51)
  {
  }
}
public record S2() : R(51)
{
}
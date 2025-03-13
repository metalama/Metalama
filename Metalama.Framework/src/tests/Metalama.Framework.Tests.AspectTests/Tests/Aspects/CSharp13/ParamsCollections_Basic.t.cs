[TheAspect]
public class Target
{
  private void ParamsArray(params global::System.Int32[] ints)
  {
  }
  private void ParamsSpan(params global::System.ReadOnlySpan<global::System.Int32> ints)
  {
  }
  private void Usage()
  {
    ParamsArray(1, 2, 3);
    ParamsSpan(1, 2, 3);
    _ = this[1, 2, 3];
  }
  private global::System.Int32 this[params global::System.Int32[] index]
  {
    get
    {
      return (global::System.Int32)0;
    }
  }
  private global::System.Int32 this[params global::System.ReadOnlySpan<global::System.Int32> index]
  {
    get
    {
      return (global::System.Int32)0;
    }
  }
}
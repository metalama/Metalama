internal class TargetCode
{
  private class Nullable
  {
    [Aspect]
    private void ReferenceType(Foo arg)
    {
      var s = arg.Nullable?.ToString();
      s = arg.NonNullable?.ToString();
      s = arg.Nullable!.ToString();
      s = arg.NonNullable!.ToString();
      var i = arg[0]?[1];
      i = arg[0]![1];
    }
    [Aspect]
    private void NullableReferenceType(Foo? arg)
    {
      var s = arg?.Nullable?.ToString();
      s = arg?.NonNullable?.ToString();
      s = arg!.Nullable!.ToString();
      s = arg!.NonNullable!.ToString();
      var i = arg?[0]?[1];
      i = arg![0]![1];
    }
  }
#nullable disable
  private class NonNullable
  {
    [Aspect]
    private void ReferenceType(Foo arg)
    {
      var s = arg?.Nullable?.ToString();
      s = arg?.NonNullable?.ToString();
      s = arg.Nullable.ToString();
      s = arg.NonNullable.ToString();
      var i = arg?[0]?[1];
      i = arg[0][1];
    }
  }
}
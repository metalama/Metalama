public class Class1
{
  public static Class1 operator +([NotNull] Class1 left, int? right)
  {
    if (left == null)
    {
      throw new global::System.ArgumentNullException();
    }
    return new();
  }
  public static Class1 operator +(int? left, [NotNull] Class1 right)
  {
    if (right == null)
    {
      throw new global::System.ArgumentNullException();
    }
    return new();
  }
}
[IntroductionAttribute]
public static class TargetType
{
  extension<T>(global::System.Collections.Generic.List<T> self)
  {
    public global::System.Boolean IsItemNull(T item)
    {
      var itemValue = item;
      return (global::System.Boolean)(itemValue == null);
    }
  }
}

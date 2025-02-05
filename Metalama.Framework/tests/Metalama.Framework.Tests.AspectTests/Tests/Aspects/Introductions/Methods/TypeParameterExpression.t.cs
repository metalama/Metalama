[Introduction]
internal class TargetClass
{
  public void DeclarativeMethod<T>()
  {
    global::System.Collections.Generic.List<T> list = new();
    list.Remove(default);
  }
}
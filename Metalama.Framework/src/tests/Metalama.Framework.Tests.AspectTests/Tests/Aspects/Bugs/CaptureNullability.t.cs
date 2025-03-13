internal class C
{
  [TheAspect]
  private void M(PropertyChangedEventArgs nonNullable, PropertyChangedEventArgs? nullable)
  {
    _ = nullable!.PropertyName;
    _ = nonNullable.PropertyName;
  }
}
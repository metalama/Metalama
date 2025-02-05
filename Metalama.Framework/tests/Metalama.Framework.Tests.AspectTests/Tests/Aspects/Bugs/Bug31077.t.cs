public class TargetClass<T> : IEnumerable<T>
{
  [TestAspect]
  public IEnumerator<T> GetEnumerator()
  {
    _ = this.GetEnumerator_Source();
    return this.GetEnumerator_Source();
  }
  private IEnumerator<T> GetEnumerator_Source()
  {
    return Enumerable.Empty<T>().GetEnumerator();
  }
  [TestAspect]
  IEnumerator IEnumerable.GetEnumerator()
  {
    _ = this.System_Collections_IEnumerable_GetEnumerator_Source();
    return this.System_Collections_IEnumerable_GetEnumerator_Source();
  }
  private IEnumerator System_Collections_IEnumerable_GetEnumerator_Source()
  {
    return GetEnumerator();
  }
}
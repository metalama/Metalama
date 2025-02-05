private string Method(object a, object b)
{
  if (a == null)
  {
    throw new global::System.ArgumentNullException("a");
  }
  if (b == null)
  {
    throw new global::System.ArgumentNullException("b");
  }
  return this.Method(a, b);
}
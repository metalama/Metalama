private string Method(string a)
{
  if (string.IsNullOrEmpty(a))
  {
    throw new global::System.ArgumentException("IsNullOrEmpty", "a");
  }
  return this.Method(a);
}
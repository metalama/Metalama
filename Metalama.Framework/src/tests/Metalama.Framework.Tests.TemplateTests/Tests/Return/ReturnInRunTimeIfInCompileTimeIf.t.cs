private object? Method(object? a)
{
  if (a == null)
  {
    return null;
  }
  return this.Method(a);
}

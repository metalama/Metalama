private object? Method(object? a)
{
  switch (a)
  {
    case null:
      throw new global::System.ArgumentNullException();
    default:
      break;
  }
  return this.Method(a);
}

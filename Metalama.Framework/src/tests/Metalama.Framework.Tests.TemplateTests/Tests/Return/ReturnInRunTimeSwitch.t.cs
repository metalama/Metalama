private object? Method(object? a)
{
  switch (a)
  {
    case null:
      return null;
    default:
      break;
  }
  return this.Method(a);
}

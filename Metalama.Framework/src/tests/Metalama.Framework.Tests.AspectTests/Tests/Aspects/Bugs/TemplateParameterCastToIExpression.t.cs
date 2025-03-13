class Target
{
  void M([Aspect] int p)
  {
    if (p > 0)
    {
      throw new global::System.ArgumentOutOfRangeException(nameof(p));
    }
  }
}
internal class Target
{
  [InsertComment]
  private void SingleAspect()
  {
    return;
  // Rewritten by aspect #1.
  }
  [InsertComment]
  [InsertComment2]
  private void TwoAspects()
  {
    return;
  // Rewritten by aspect #2.
  // Rewritten by aspect #1.
  }
  [InsertComment]
  [InsertComment2]
  private void TwoAspectsWithReturn()
  {
    return;
  // Rewritten by aspect #2.
  // Rewritten by aspect #1.
  }
}

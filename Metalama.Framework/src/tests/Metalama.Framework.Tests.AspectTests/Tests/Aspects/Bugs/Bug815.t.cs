internal class Target
{
  [InsertComment]
  private void SingleAspect()
  {
    // Rewritten by aspect #1.
    return;
  }
  [InsertComment]
  [InsertComment2]
  private void TwoAspects()
  {
    // Rewritten by aspect #1.
    // Rewritten by aspect #2.
    return;
  }
  [InsertComment]
  [InsertComment2]
  private void TwoAspectsWithReturn()
  {
    // Rewritten by aspect #1.
    // Rewritten by aspect #2.
    return;
  }
}

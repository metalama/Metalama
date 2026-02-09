internal class Target
{
  [InsertComment]
  private void SingleAspect()
  {
  // Rewritten by aspect #1.
  }
  [InsertComment]
  [InsertComment2]
  private void TwoAspects()
  {
  // Rewritten by aspect #2.
  // Rewritten by aspect #1.
  }
  [InsertComment]
  [InsertComment2]
  private void TwoAspectsWithReturn()
  {
  // Rewritten by aspect #2.
  // Rewritten by aspect #1.
  }
}

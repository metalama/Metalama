public class Caller
{
  public void Method()
  {
    // Object initializer with inheritance — no cast needed with generic Initialized<T>
    var sample = new RadioactiveSample
    {
      HalfLifeSeconds = 1.808e11,
      AtomicNumber = 6,
      MassNumber = 14,
      InitialAtoms = 1e24
    }.WithInitialize();
    // with expression — recomputes derived values
    var adjusted = (sample with
    {
      InitialAtoms = 5e23
    }
    ).WithInitialize(InitializationMetadata.Modify);
  }
}
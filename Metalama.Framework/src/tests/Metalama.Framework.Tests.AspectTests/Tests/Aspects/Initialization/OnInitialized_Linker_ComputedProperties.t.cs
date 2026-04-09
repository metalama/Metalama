public class Caller
{
  public void Method()
  {
    // Object initializer with inheritance — no cast needed with generic Initialized<T>
    var sample = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new RadioactiveSample { HalfLifeSeconds = 1.808e11, AtomicNumber = 6, MassNumber = 14, InitialAtoms = 1e24 });
    // with expression — recomputes derived values
    var adjusted = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize((sample with { InitialAtoms = 5e23 }), global::Metalama.Framework.RunTime.Initialization.InitializationMetadata.Modify);
  }
}
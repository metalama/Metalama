public class Caller
{
  private X _x = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new X { Y = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new Y { Z = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new Z()) }) });
}
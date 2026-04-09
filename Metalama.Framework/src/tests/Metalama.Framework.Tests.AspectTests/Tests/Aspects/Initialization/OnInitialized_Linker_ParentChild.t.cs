public class Caller
{
  public void Method()
  {
    // Nested object initializers — all get .Initialize()
    var tree = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new TreeNode { Name = "root", Children = new[] { global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new TreeNode { Name = "left" }), global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new TreeNode { Name = "right" }) } });
  }
}
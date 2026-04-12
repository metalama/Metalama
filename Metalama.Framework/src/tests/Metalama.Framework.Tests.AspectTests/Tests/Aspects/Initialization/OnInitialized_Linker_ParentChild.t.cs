public class Caller
{
  public void Method()
  {
    // Nested object initializers — all get .Initialize()
    var tree = new TreeNode
    {
      Name = "root",
      Children = new[]
      {
        new TreeNode
        {
          Name = "left"
        }.WithInitialize(),
        new TreeNode
        {
          Name = "right"
        }.WithInitialize()
      }
    }.WithInitialize();
  }
}
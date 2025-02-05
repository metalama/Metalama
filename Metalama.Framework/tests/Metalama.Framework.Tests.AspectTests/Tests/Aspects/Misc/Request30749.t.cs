public class OrderAsyncResponseProcessor<TCommand> : BaseProcessor where TCommand : OrderCommand
{
  private readonly ILogger _logger = null !;
  [LogExceptionContextAspect]
  protected override async Task HandleFailureAsync(object response, object context)
  {
    var __metalma_currentThreadId = global::System.Threading.Thread.CurrentThread.ManagedThreadId;
    var __metalama_result = new global::System.Text.StringBuilder();
    __metalama_result.Append("OrderAsyncResponseProcessor<TCommand>");
    __metalama_result.Append(".");
    __metalama_result.Append("HandleFailureAsync");
    __metalama_result.Append("(");
    __metalama_result.Append("response = {");
    var __metalama_json = string.Empty;
    try
    {
      __metalama_json = "";
    }
    catch
    {
      __metalama_json = $"{response}";
    }
    __metalama_result.Append(__metalama_json);
    __metalama_result.Append("}");
    __metalama_result.Append(", context = {");
    var __metalama_json_1 = string.Empty;
    try
    {
      __metalama_json_1 = "";
    }
    catch
    {
      __metalama_json_1 = $"{context}";
    }
    __metalama_result.Append(__metalama_json_1);
    __metalama_result.Append("}");
    __metalama_result.Append(")");
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Request30749.ParamsStacks.Push(__metalma_currentThreadId, __metalama_result.ToString());
    try
    {
      await this.HandleFailureAsync_Source(response, context);
      return;
    }
    finally
    {
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Request30749.ParamsStacks.Pop(__metalma_currentThreadId);
    }
  }
  private async Task HandleFailureAsync_Source(object response, object context)
  {
    _logger.WriteInfo($"Handling failure response");
    await base.HandleFailureAsync(response, context);
    _logger.WriteInfo($"Handled failure response ");
  }
}
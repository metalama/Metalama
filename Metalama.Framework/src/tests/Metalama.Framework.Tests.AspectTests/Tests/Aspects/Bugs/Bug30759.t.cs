public class OrderAsyncResponseProcessor<TCommand> : AsyncResponseProcessor<Response, TCommand> where TCommand : OrderCommand
{
  private readonly ILogger _logger;
  public OrderAsyncResponseProcessor(IEntityWriter writer, ILogger logger) : base(writer)
  {
    _logger = logger;
  }
  [LogExceptionContextAspect]
  protected override async Task HandleFailureAsync(OrderResponse response, ICommandExecutionContext context)
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
      __metalama_json = global::Newtonsoft.Json.JsonConvert.SerializeObject(response);
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
      __metalama_json_1 = global::Newtonsoft.Json.JsonConvert.SerializeObject(context);
    }
    catch
    {
      __metalama_json_1 = $"{context}";
    }
    __metalama_result.Append(__metalama_json_1);
    __metalama_result.Append("}");
    __metalama_result.Append(")");
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug30840.ParamsStacks.Push(__metalma_currentThreadId, __metalama_result.ToString());
    try
    {
      await this.HandleFailureAsync_Source(response, context);
      object result = null;
      return;
    }
    finally
    {
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug30840.ParamsStacks.Pop(__metalma_currentThreadId);
    }
  }
  private async Task HandleFailureAsync_Source(OrderResponse response, ICommandExecutionContext context)
  {
    _logger.WriteInfo($"Handling failure response");
    await base.HandleFailureAsync(response, context);
    _logger.WriteInfo($"Handled failure response ");
  }
}
// Warning LAMA0661 on `id`: `The lambda parameter 'id' is renamed to 'id_1' in the compiled code, because it hides the parameter 'id' of 'Program.Handle(int, int[])', and this parameter is passed by the call 'Source.Log( $"item {id} {new { id }.id}" )' redirected to 'Interceptors.Log(string, int)' by aspect [RedirectAttribute] applied to 'Program'. The compiled code no longer matches the source code, so the debugger shows the new name. Rename the lambda parameter in the source code to remove this warning.`
// Warning LAMA0661 on `id`: `The local variable 'id' is renamed to 'id_1' in the compiled code, because it hides the parameter 'id' of 'Program.Handle(int, int[])', and this parameter is passed by the call 'Source.Log( $"end {id}" )' redirected to 'Interceptors.Log(string, int)' by aspect [RedirectAttribute] applied to 'Program'. The compiled code no longer matches the source code, so the debugger shows the new name. Rename the local variable in the source code to remove this warning.`
[Redirect]
internal static class Program
{
  private static void Handle(int id, int[] items)
  {
    global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_HiddenParameter.Interceptors.Log(message: "start", requestId: id);
    Array.ForEach(items, id_1 => global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_HiddenParameter.Interceptors.Log(message: $"item {id_1} {new { id = id_1 }.id}", requestId: ((global::System.Int32)id)));
    Action end = () =>
    {
      var id_1 = 99;
      global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_HiddenParameter.Interceptors.Log(message: $"end {id_1}", requestId: ((global::System.Int32)id));
    };
    end();
  }
  public static void TestMain() => Handle(7, new[] { 1, 2 });
}
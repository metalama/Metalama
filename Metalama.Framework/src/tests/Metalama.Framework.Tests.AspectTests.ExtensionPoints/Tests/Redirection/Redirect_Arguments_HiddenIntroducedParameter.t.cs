// Warning LAMA0661 on `id`: `The lambda parameter 'id' is renamed to 'id_1' in the compiled code, because it hides the parameter 'id' of 'Program.Program(int[])', and this parameter is passed by the call 'Source.Log( $"item {id}" )' redirected to 'Interceptors.Log(string, int)' by aspect [RedirectAttribute] applied to 'Program'. The compiled code no longer matches the source code, so the debugger shows the new name. Rename the lambda parameter in the source code to remove this warning.`
[Redirect]
internal class Program
{
  public Program(int[] items, global::System.Int32 id = 7)
  {
    global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_HiddenIntroducedParameter.Interceptors.Log(message: "start", requestId: ((global::System.Int32)id));
    Array.ForEach(items, id_1 => global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_HiddenIntroducedParameter.Interceptors.Log(message: $"item {id_1}", requestId: ((global::System.Int32)id)));
  }
  public static void TestMain() => _ = new Program(new[] { 1, 2 });
}
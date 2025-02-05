using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Combined.CompileTimeDeclarationBlock;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        // This tests that statements that cannot be embedded are not embedded in template code.

        foreach (var member in meta.Target.Type.Methods)
        {
            var id = member.Name;
        }

        if (meta.Target.Method is { } method)
        {
            var id = method.Name;
        }

        return null!;
    }
}

internal class TargetCode
{
    private void Method() { }
}
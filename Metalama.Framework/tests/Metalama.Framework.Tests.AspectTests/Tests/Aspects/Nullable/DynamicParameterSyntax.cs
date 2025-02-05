#if TEST_OPTIONS
// @ClearIgnoredDiagnostics to verify nullability warnings
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Nullable.DynamicParameterQuestionMarkSyntax;

internal class Aspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override( nameof(Template) );
    }

    [Template]
    private void Template( dynamic? arg )
    {
        var s = arg?.Nullable?.ToString();
        s = arg?.NonNullable?.ToString();

        s = arg!.Nullable!.ToString();
        s = arg!.NonNullable!.ToString();

        var i = arg?[0]?[1];
        i = arg![0]![1];
    }
}

internal class Foo
{
    public object? Nullable = null;
    public object NonNullable = null!;

    public int[] this[ int i ] => null!;
}

// <target>
internal class TargetCode
{
    private class Nullable
    {
        [Aspect]
        private void ReferenceType( Foo arg ) { }

        [Aspect]
        private void NullableReferenceType( Foo? arg ) { }
    }

#nullable disable

    private class NonNullable
    {
        [Aspect]
        private void ReferenceType( Foo arg ) { }
    }
}
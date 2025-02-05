using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.ErrorNestedTypeNotFabric
{
    // <target>
    internal class TargetCode
    {
        [CompileTime]
        private enum E { }

        [CompileTime]
        private interface I { }

        [CompileTime]
        private struct S { }

        [CompileTime]
        private delegate void D();

        [CompileTime]
        private record R( int x );

        [CompileTime]
        private class C { }

        [RunTimeOrCompileTime]
        private class C2 { }
    }
}
using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS8603

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.CompileTimeParameters
{
    internal class MyAspect : TypeAspect
    {
        [Template]
        public (T?, Type) Template<[CompileTime] T>( IDeclaration d, T t, int i )
        {
            return (default, typeof(T));
        }
    }
}
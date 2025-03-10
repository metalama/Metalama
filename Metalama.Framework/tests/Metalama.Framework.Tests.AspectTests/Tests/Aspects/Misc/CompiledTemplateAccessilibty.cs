// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.CompiledTemplateAccessilibty;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(Private), args: new { x = new PrivateCompileTimeType() } );
        builder.IntroduceMethod( nameof(Protected), args: new { x = new ProtectedCompileTimeType() } );
        builder.IntroduceMethod( nameof(Internal), args: new { x = new InternalCompileTimeType() } );
        builder.IntroduceMethod( nameof(Public), args: new { x = new PublicCompileTimeType() } );
        builder.IntroduceMethod( nameof(PrivateProtected), args: new { x = new ProtectedCompileTimeType(), y = new InternalCompileTimeType() } );
        builder.IntroduceMethod( nameof(ProtectedInternal), args: new { x = new ProtectedInternalCompileTimeType() } );
    }

    [Template]
    private string Private( PrivateCompileTimeType x ) => $"{x}";

    // TODO: call Tostring everywhere else

    [Template]
    protected string Protected( ProtectedCompileTimeType x ) => $"{x}";

    [Template]
    internal string Internal( InternalCompileTimeType x ) => $"{x}";

    [Template]
    public string Public( PublicCompileTimeType x ) => $"{x}";

    [Template]
    private protected string PrivateProtected( ProtectedCompileTimeType x, InternalCompileTimeType y ) => $"{x}, {y}";

    [Template]
    protected internal string ProtectedInternal( ProtectedInternalCompileTimeType x ) => $"{x}";

    [CompileTime]
    private class PrivateCompileTimeType { }

    [CompileTime]
    protected class ProtectedCompileTimeType { }

    [CompileTime]
    protected internal class ProtectedInternalCompileTimeType { }
}

[CompileTime]
internal class InternalCompileTimeType { }

[CompileTime]
public class PublicCompileTimeType { }

// <target>
[MyAspect]
internal class Target { }
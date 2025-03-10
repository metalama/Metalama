// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code;

[InternalImplement]
[CompileTime]
public interface ISourceReferenceImpl
{
    string GetKind( in SourceReference sourceReference );

    SourceSpan GetSourceSpan( in SourceReference sourceReference );

    string GetText( in SourceSpan sourceSpan );

    string GetText( in SourceReference sourceReference, bool normalized );

    bool IsImplementationPart( in SourceReference sourceReference );
}
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CompileTimeMethodTemplateParameter;

internal class AspectAttribute : TypeAspect
{
    [Template]
    private void Template1( int a ) => IntMethod( a );

    [CompileTime]
    private int IntMethod( int i ) => i;

    [Template]
    private void Template2( int b ) => VoidMethod( b );

    [CompileTime]
    private void VoidMethod( int j ) { }
}
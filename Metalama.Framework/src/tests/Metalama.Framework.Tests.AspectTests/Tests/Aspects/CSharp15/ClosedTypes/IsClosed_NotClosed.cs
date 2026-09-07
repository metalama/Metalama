// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// This test carries no directive, so it runs in both Roslyn variants and whether or not the preview opt-in of
// eng/RoslynPreview.props is set. It pins that INamedType.IsClosed is false for an ordinary abstract class, and
// that reading the property reports no diagnostic in a variant whose Roslyn does not declare ITypeSymbol.IsClosed.
// IsClosed.cs is the counterpart of this test, on a class that is closed.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.ClosedTypes.IsClosed_NotClosed
{
    public class ReportClosednessAttribute : TypeAspect
    {
        [Introduce]
        public bool IsClosed => meta.Target.Type.IsClosed;
    }

    // <target>
    [ReportClosedness]
    public abstract class AbstractTarget { }
}

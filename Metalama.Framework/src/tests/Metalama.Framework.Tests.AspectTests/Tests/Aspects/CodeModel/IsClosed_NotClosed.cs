// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// This test carries no directive and lives outside the CSharp15 folder, whose metalamaTests.json requires the latest
// Roslyn variant, so it runs in both variants and whether or not the preview opt-in of eng/RoslynPreview.props is
// set. It pins that INamedType.IsClosed is false for an ordinary abstract class, and that reading the property
// reports no diagnostic in a variant whose Roslyn does not declare ITypeSymbol.IsClosed. The counterpart on a class
// that is closed is Tests/Aspects/CSharp15/ClosedTypes/IsClosed.cs.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CodeModel.IsClosed_NotClosed
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

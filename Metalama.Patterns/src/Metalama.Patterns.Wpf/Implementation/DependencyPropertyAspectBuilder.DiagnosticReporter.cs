// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;

namespace Metalama.Patterns.Wpf.Implementation;

internal sealed partial class DependencyPropertyAspectBuilder
{
    [CompileTime]
    private sealed class DiagnosticReporter : NamingConvention.DiagnosticReporter
    {
        public DiagnosticReporter( IAspectBuilder builder ) : base( builder ) { }

        protected override string GetInvalidityReason( in InspectedMember inspectedMember ) => " Refer to documentation for supported method signatures.";

        protected override string GeneratedArtifactKind => "dependency property";

        protected override string GetTargetDeclarationDescription() => "[DependencyProperty] property ";
    }
}
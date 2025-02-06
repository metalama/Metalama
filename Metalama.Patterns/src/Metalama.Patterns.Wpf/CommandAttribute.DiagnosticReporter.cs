// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;

namespace Metalama.Patterns.Wpf;

public sealed partial class CommandAttribute
{
    [CompileTime]
    private sealed class DiagnosticReporter : Implementation.NamingConvention.DiagnosticReporter
    {
        public DiagnosticReporter( IAspectBuilder builder ) : base( builder ) { }

        protected override string GetInvalidityReason( in InspectedMember inspectedMember )
            => inspectedMember.Member.DeclarationKind == DeclarationKind.Property
                ? " The property must be of type bool and have a getter."
                : " The method must not be generic, must return bool and may optionally have a single parameter of any type, but which must not be a ref or out parameter.";

        protected override string GeneratedArtifactKind => "command";

        protected override string GetTargetDeclarationDescription() => "[Command] method ";
    }
}
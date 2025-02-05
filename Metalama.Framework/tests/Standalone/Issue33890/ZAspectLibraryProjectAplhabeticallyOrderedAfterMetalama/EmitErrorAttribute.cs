// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace AspectLibraryProject
{
    public class EmitErrorAttribute : OverrideMethodAspect
    {
        private static readonly DiagnosticDefinition _error = new(
            "MY001",
            Severity.Error,
            "My error.");

        public override void BuildAspect(IAspectBuilder<IMethod> builder)
        {
            builder.Diagnostics.Report(_error);
        }

        public override dynamic OverrideMethod()
        {
            throw new NotImplementedException();
        }
    }
}
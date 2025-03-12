// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
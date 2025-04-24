// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.TestApp.Aspects;

namespace Metalama.Framework.TestApp
{
    public class PrintDebugInfoAspect : MethodAspect
    {
        static DiagnosticDefinition<IDeclaration> myWarning = new("MY001", Severity.Warning, "Hello, {0} v24.");

        public override void BuildAspect(IAspectBuilder<IMethod> aspectBuilder)
        {
            aspectBuilder.Advice.Override(aspectBuilder.Target, nameof(OverrideMethod));
            aspectBuilder.Diagnostics.Report(myWarning.WithArguments(aspectBuilder.Target));
        }

        // The template is intentionally private to reproduce #30575.
        [Template]
        private dynamic? OverrideMethod()
        {
            Console.WriteLine(DebugInfo.GetInfo());
            return meta.Proceed();
        }
    }
}

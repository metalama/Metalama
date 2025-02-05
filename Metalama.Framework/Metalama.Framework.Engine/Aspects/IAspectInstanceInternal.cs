// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Aspects;

internal interface IAspectInstanceInternal : IAspectInstance, IAspectPredecessorImpl, IDiagnosticSource, IAspectDeclarationOrigin
{
    void Skip();

    ImmutableDictionary<TemplateClass, TemplateClassInstance> TemplateInstances { get; }

    void SetState( IAspectState? value );

    new IAspectClassImpl AspectClass { get; }
}
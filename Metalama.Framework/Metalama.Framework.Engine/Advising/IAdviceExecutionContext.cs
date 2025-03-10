// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Introspection;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Transformations;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Advising;

internal interface IAdviceExecutionContext
{
    CompilationModel MutableCompilation { get; }

    IAspectInstanceInternal AspectInstance { get; }

    ref readonly ProjectServiceProvider ServiceProvider { get; }

    IDiagnosticAdder Diagnostics { get; }

    IntrospectionPipelineListener? IntrospectionListener { get; }
    
    void AddTransformations( List<ITransformation> transformations );

    void SetOrders( ITransformation transformation );
}
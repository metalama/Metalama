// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
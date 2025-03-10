// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Introspection;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Introspection;

[PublicAPI]
public sealed class IntrospectionCompiler
{
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly IIntrospectionOptionsProvider? _options;

    public IntrospectionCompiler( in ProjectServiceProvider serviceProvider, IIntrospectionOptionsProvider? options = null )
    {
        this._serviceProvider = serviceProvider;
        this._options = options;
    }

    public async Task<IIntrospectionCompilationResult> CompileAsync( ICompilation compilation )
    {
        var compilationModel = (CompilationModel) compilation;
        var pipeline = new IntrospectionAspectPipeline( this._serviceProvider, this._options );

        return await pipeline.ExecuteAsync( compilationModel, TestableCancellationToken.None );
    }
}
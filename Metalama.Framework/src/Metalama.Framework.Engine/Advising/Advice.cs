// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;

namespace Metalama.Framework.Engine.Advising;

internal abstract class Advice : IDiagnosticSource
{
    public AspectLayerInstance AspectLayerInstance { get; }

    public IAspectInstanceInternal AspectInstance => this.AspectLayerInstance.AspectInstance;

    public AspectLayerId AspectLayerId => this.AspectLayerInstance.AspectLayerId;

    protected TemplateClassInstance TemplateInstance { get; }

    public IDeclaration TargetDeclaration { get; }

    /// <summary>
    /// Gets the compilation from which the advice was instantiated.
    /// </summary>
    protected CompilationModel SourceCompilation => this.AspectLayerInstance.InitialCompilation;

    public abstract AdviceKind AdviceKind { get; }

    protected Advice( AdviceConstructorParameters parameters )
    {
#if DEBUG
        if ( parameters.TargetDeclaration.DeclaringAssembly.IsExternal )
        {
            throw new AssertionFailedException( $"Cannot override '{parameters.TargetDeclaration}' because it is external." );
        }
#endif
        this.AspectLayerInstance = parameters.AspectLayerInstance;
        this.TemplateInstance = parameters.TemplateInstance;
        this.TargetDeclaration = parameters.TargetDeclaration;
    }

    string IDiagnosticSource.DiagnosticSourceDescription => $"{this.GetType().Name} supplied by {this.AspectInstance.DiagnosticSourceDescription}'";

    /// <summary>
    /// Parameter object containing parameters shared by constructors of all advice types.
    /// </summary>
    public record struct AdviceConstructorParameters(
        AspectLayerInstance AspectLayerInstance,
        TemplateClassInstance TemplateInstance,
        IDeclaration TargetDeclaration );

    /// <summary>
    /// Generic version of parameter object containing parameters shared by constructors of all advice types.
    /// </summary>
    public record struct AdviceConstructorParameters<T>(
        AspectLayerInstance AspectLayerInstance,
        TemplateClassInstance TemplateClassInstance,
        T TargetDeclaration )
        where T : IDeclaration
    {
        public static implicit operator AdviceConstructorParameters( AdviceConstructorParameters<T> parameters )
            => new(
                parameters.AspectLayerInstance,
                parameters.TemplateClassInstance,
                parameters.TargetDeclaration );
    }
}
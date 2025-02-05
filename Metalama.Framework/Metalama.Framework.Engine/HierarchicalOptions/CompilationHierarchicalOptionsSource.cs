// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attribute = System.Attribute;

namespace Metalama.Framework.Engine.HierarchicalOptions;

internal sealed class CompilationHierarchicalOptionsSource : IHierarchicalOptionsSource
{
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly UserCodeAttributeDeserializer.Provider _attributeDeserializerProvider;
    private readonly UserCodeInvoker _invoker;

    public CompilationHierarchicalOptionsSource( in ProjectServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._attributeDeserializerProvider = serviceProvider.GetRequiredService<UserCodeAttributeDeserializer.Provider>();
        this._invoker = serviceProvider.GetRequiredService<UserCodeInvoker>();
    }

    public Task CollectOptionsAsync(
        CompilationModel compilation,
        Action<HierarchicalOptionsInstance> addOptions,
        IUserDiagnosticSink diagnosticSink,
        CancellationToken cancelationToken )
    {
        var aspectType = compilation.Factory.GetTypeByReflectionType( typeof(IAspect) );
        var systemAttributeType = compilation.Factory.GetTypeByReflectionType( typeof(Attribute) );

        var attributeDeserializer = this._attributeDeserializerProvider.Get( compilation.CompilationContext );

        foreach ( var attributeType in compilation.GetDerivedTypes(
                     (INamedType) compilation.Factory.GetTypeByReflectionType( typeof(IHierarchicalOptionsProvider) ),
                     DerivedTypesOptions.IncludingExternalTypesDangerous ) )
        {
            if ( attributeType.IsConvertibleTo( aspectType ) )
            {
                // Aspects can implement IHierarchicalOptionsProvider but their options are exposed on IAspectInstance.GetOptions
                // or IAspectBuilder.Options and are not handled by the current facility. Because we want options defined
                // on aspects to have absolute priority.
                continue;
            }
            else if ( !attributeType.IsConvertibleTo( systemAttributeType ) )
            {
                continue;
            }

            foreach ( var attribute in compilation.GetAllAttributesOfType( attributeType ) )
            {
                if ( !attributeDeserializer.TryCreateAttribute( attribute.GetAttributeData(), diagnosticSink, out var deserializedAttribute ) )
                {
                    continue;
                }

                var optionsAttribute = (IHierarchicalOptionsProvider) deserializedAttribute;

                var invokerContext = new UserCodeExecutionContext(
                    this._serviceProvider,
                    UserCodeDescription.Create( "executing GetOptions() for '{0}' applied to '{1}'", attributeType.Name, attribute.ContainingDeclaration ),
                    compilation.CompilationContext,
                    targetDeclaration: attribute.ContainingDeclaration );

                var providerContext = new OptionsProviderContext(
                    attribute.ContainingDeclaration,
                    new ScopedDiagnosticSink(
                        diagnosticSink,
                        new ProvideOptionsDiagnosticSource( attribute ),
                        attribute,
                        attribute.ContainingDeclaration ) );

                var optionList = this._invoker.Invoke( () => optionsAttribute.GetOptions( providerContext ).ToReadOnlyList(), invokerContext );

                foreach ( var options in optionList )
                {
                    addOptions( new HierarchicalOptionsInstance( attribute.ContainingDeclaration, options ) );
                }
            }
        }

        return Task.CompletedTask;
    }

    private sealed class ProvideOptionsDiagnosticSource : IDiagnosticSource
    {
        private readonly IAttribute _attribute;

        public ProvideOptionsDiagnosticSource( IAttribute attribute )
        {
            this._attribute = attribute;
        }

        public string DiagnosticSourceDescription
            => $"executing '{this._attribute.Type.Name}.{nameof(IHierarchicalOptionsProvider.GetOptions)}' for '{this._attribute.ContainingDeclaration}'";
    }
}
// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Maintenance;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Introductions.Helpers;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Serialization;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Introspection.References;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Engine.Services;

public static class ServiceProviderFactory
{
    private static readonly AsyncLocal<ServiceProviderFactoryConfiguration?> _asyncLocalConfiguration = new();
    private static ServiceProvider<IGlobalService>? _globalInstance;

    static ServiceProviderFactory()
    {
        MetalamaEngineModuleInitializer.EnsureInitialized();
    }

    /// <summary>
    /// Gets or sets the <see cref="AdditionalServiceCollection"/> that will be used by the <see cref="GetServiceProvider(System.IServiceProvider?,Metalama.Framework.Engine.Services.AdditionalServiceCollection?)"/> method if
    /// none is supplied by the caller of this method.
    /// </summary>
    [PublicAPI]
    public static ServiceProviderFactoryConfiguration? AsyncLocalConfiguration
    {
        get => _asyncLocalConfiguration.Value;
        set => _asyncLocalConfiguration.Value = value;
    }

    /// <summary>
    /// Gets an instance of <see cref="ServiceProvider{TBase}"/> with a specific upstream <see cref="IServiceProvider"/>.
    /// </summary>
    public static ServiceProvider<IGlobalService> GetServiceProvider(
        IServiceProvider? upstreamServiceProvider,
        AdditionalServiceCollection? additionalServices = null )
    {
        upstreamServiceProvider ??= _asyncLocalConfiguration.Value?.NextProvider ?? BackstageServiceFactory.ServiceProvider;
        additionalServices ??= _asyncLocalConfiguration.Value?.AdditionalServices;

        var serviceProvider = ServiceProvider<IGlobalService>.Empty.WithNextProvider( upstreamServiceProvider );

        if ( additionalServices != null )
        {
            // We hook both the mocked services and the MockFactory itself, so that other levels of factory method
            // know about them.
            serviceProvider = additionalServices.GlobalServices.Build( serviceProvider ).WithService( additionalServices );
        }

        serviceProvider = serviceProvider
            .WithServiceConditional<ITaskRunner>( _ => new TaskRunner() )
            .WithServiceConditional<IGlobalOptions>( _ => new DefaultGlobalOptions() )
            .WithServiceConditional<ITestableCancellationTokenSourceFactory>( _ => new DefaultTestableCancellationTokenSource() )
            .WithServiceConditional<ICompileTimeDomainFactory>( sp => new DefaultCompileTimeDomainFactory( sp ) )
            .WithServiceConditional<IMetalamaProjectClassifier>( _ => new MetalamaProjectClassifier() )
            .WithServiceConditional( sp => new UserCodeInvoker( sp ) )
            .WithServiceConditional<IReferenceAssemblyLocatorProvider>(
                sp => new ReferenceAssemblyLocatorProvider( sp.GetRequiredBackstageService<ITempFileManager>() ) )
            .WithServiceConditional( _ => new FrameworkCompileTimeProjectFactory() )
            .WithServiceConditional( _ => new AttributeClassificationService() )
            .WithServiceConditional<IProjectOptionsFactory>( _ => new MSBuildProjectOptionsFactory() );

        return serviceProvider;
    }

    /// <summary>
    /// Gets the default <see cref="ServiceProvider{TBase}"/> instance.
    /// </summary>
    public static ServiceProvider<IGlobalService> GetServiceProvider()
        => _asyncLocalConfiguration.Value == null
            ? LazyInitializer.EnsureInitialized(
                ref _globalInstance,
                () => GetServiceProvider( null ) )
            : GetServiceProvider( null );

    public static ServiceProvider<IProjectService> WithProjectScopedServices(
        this IServiceProvider<IGlobalService> serviceProvider,
        IProjectOptions projectOptions,
        Compilation compilation )
        => serviceProvider.WithProjectScopedServices( projectOptions, compilation.References.OfType<PortableExecutableReference>() );

    /// <summary>
    /// Adds the services that have the same scope as the project processing itself.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="projectOptions"></param>
    /// <param name="metadataReferences">A list of resolved metadata references for the current project.
    /// These can only be PortableExecutableReferences and not CompilationReferences, because a CompilationReference would keep a reference to a Compilation.</param>
    public static ServiceProvider<IProjectService> WithProjectScopedServices(
        this IServiceProvider<IGlobalService> serviceProvider,
        IProjectOptions projectOptions,
        IEnumerable<PortableExecutableReference> metadataReferences )
    {
        var projectServiceProvider = ServiceProvider<IProjectService>.Empty.WithNextProvider( serviceProvider ).WithService( projectOptions );

        var additionalServices = serviceProvider.GetService<AdditionalServiceCollection>();

        if ( additionalServices != null )
        {
            projectServiceProvider = additionalServices.ProjectServices.Build( projectServiceProvider );
        }

        if ( projectServiceProvider.GetService<IConcurrentTaskRunner>() == null )
        {
            // We use a single-threaded task scheduler for tests because the test runner itself is already multi-threaded and
            // most tests are so small that they do not allow for significant concurrency anyway. A specific test can provide a different scheduler.
            // We randomize the ordering of execution to improve the test relevance.
            IConcurrentTaskRunner concurrentTaskRunner;

            if ( projectOptions.IsTest )
            {
                concurrentTaskRunner = new RandomizingSingleThreadedTaskRunner( ((ProjectServiceProvider) projectServiceProvider).Global );
            }
            else
            {
                concurrentTaskRunner = projectOptions.IsConcurrentBuildEnabled ? new ConcurrentTaskRunner() : new SingleThreadedTaskRunner();
            }

            projectServiceProvider = projectServiceProvider.WithService( concurrentTaskRunner );
        }

        projectServiceProvider = projectServiceProvider
            .WithServiceConditional<SerializerFactoryProvider>( sp => new BuiltInSerializerFactoryProvider( sp ) )
            .WithServiceConditional<IDeserializationSurrogateProvider>( _ => new DeserializationSurrogateProvider() )
            .WithServiceConditional<IAssemblyLocator>( sp => new AssemblyLocator( sp, metadataReferences ) )
            .WithService( _ => new SyntaxSerializationService() )
            .WithServiceConditional( sp => new SystemTypeResolver.Provider( sp ) )
            .WithServiceConditional( sp => new SystemAttributeDeserializer.Provider( sp ) )
            .WithService( provider => new ClassifyingCompilationContextFactory( provider ) )
            .WithService( provider => new ObjectReaderFactory( provider ) )
            .WithService( provider => new ProjectIntrospectionService( provider ) )
            .WithService( provider => new SourceGeneratorDetectionService( provider ) );

        if ( projectOptions.FormatCompileTimeCode || projectOptions.CodeFormattingOptions == CodeFormattingOptions.Formatted || projectOptions.WriteHtml )
        {
            projectServiceProvider = projectServiceProvider.WithService( sp => new CodeFormatter( sp ) );
        }

        return projectServiceProvider;
    }

    internal static ServiceProvider<IProjectService> WithCompileTimeProjectServices(
        this ProjectServiceProvider serviceProvider,
        CompileTimeProjectRepository repository )
    {
        return serviceProvider.Underlying
            .WithService( repository )
            .WithService<CompilationServiceProvider<ProjectSpecificCompileTimeTypeResolver>>( sp => new ProjectSpecificCompileTimeTypeResolver.Provider( sp ) )
            .WithServiceConditional<UserCodeAttributeDeserializer.Provider>( sp => new UserCodeAttributeDeserializer.Provider( sp ) )
            .WithService<SymbolClassificationService>( _ => new SymbolClassificationService( repository ) )
            .WithServiceConditional<TemplateAttributeFactory>( sp => new TemplateAttributeFactory( sp ) )
            .WithService( sp => new TemplateClassMemberBuilder( sp ) );
    }
}
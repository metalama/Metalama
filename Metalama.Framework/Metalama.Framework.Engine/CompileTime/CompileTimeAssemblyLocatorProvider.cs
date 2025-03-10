// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Maintenance;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// A global service that provides an instance of <see cref="CompileTimeAssemblyLocator"/>, which itself is
/// a project-scoped service but that can and should be shared among projects that have the same <see cref="IProjectOptions.CompileTimePackages"/>.
/// </summary>
public sealed class CompileTimeAssemblyLocatorProvider : ICompileTimeAssemblyLocatorProvider
{
    private readonly object _sync = new();
    private readonly ITempFileManager _tempFileManager;

    private volatile ImmutableDictionary<string, CompileTimeAssemblyLocator> _referenceAssemblyLocators =
        ImmutableDictionary<string, CompileTimeAssemblyLocator>.Empty;

    public CompileTimeAssemblyLocatorProvider( ITempFileManager tempFileManager )
    {
        // We intentionally explicitly require to specify ITempFileManager because its origin is different in production
        // than in tests, where there is one TempFileManager per TestContext, while it is essential for performance to have a
        // share the directory among all instances.
        this._tempFileManager = tempFileManager;
    }

    CompileTimeAssemblyLocator ICompileTimeAssemblyLocatorProvider.GetInstance( in ProjectServiceProvider serviceProvider )
    {
        var projectOptions = serviceProvider.GetRequiredService<IProjectOptions>();

        var additionalReferences = CompileTimeAssemblyLocator.GetAdditionalReferences( projectOptions );

        if ( !this._referenceAssemblyLocators.TryGetValue( additionalReferences, out var referenceAssemblyLocator ) )
        {
            // We lock instead of using ConcurrentDictionary because instantiating the class is expensive.
            lock ( this._sync )
            {
                if ( !this._referenceAssemblyLocators.TryGetValue( additionalReferences, out referenceAssemblyLocator ) )
                {
                    referenceAssemblyLocator = new CompileTimeAssemblyLocator( serviceProvider, additionalReferences, this._tempFileManager );
                    this._referenceAssemblyLocators = this._referenceAssemblyLocators.Add( additionalReferences, referenceAssemblyLocator );
                }
            }
        }

        return referenceAssemblyLocator;
    }
}
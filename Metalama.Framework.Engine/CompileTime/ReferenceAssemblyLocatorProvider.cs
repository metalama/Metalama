// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Maintenance;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// A global service that provides an instance of <see cref="ReferenceAssemblyLocator"/>, which itself is
/// a project-scoped service but that can and should be shared among projects that have the same <see cref="IProjectOptions.CompileTimePackages"/>.
/// </summary>
public sealed class ReferenceAssemblyLocatorProvider : IReferenceAssemblyLocatorProvider 
{
    private readonly object _sync = new();
    private readonly ITempFileManager _tempFileManager; 

    private volatile ImmutableDictionary<string, ReferenceAssemblyLocator> _referenceAssemblyLocators =
        ImmutableDictionary<string, ReferenceAssemblyLocator>.Empty;

    public ReferenceAssemblyLocatorProvider( ITempFileManager tempFileManager )
    {
        // We intentionally explicitly require to specify ITempFileManager because its origin is different in production
        // than in tests, where there is one TempFileManager per TestContext, while it is essential for performance to have a
        // share the directory among all instances.
        this._tempFileManager = tempFileManager;
    }

    ReferenceAssemblyLocator IReferenceAssemblyLocatorProvider.GetInstance( in ProjectServiceProvider serviceProvider )
    {
        var projectOptions = serviceProvider.GetRequiredService<IProjectOptions>();

        var additionalPackageReferences = projectOptions.CompileTimePackages.IsDefaultOrEmpty
            ? string.Empty
            : ReferenceAssemblyLocator.GetAdditionalPackageReferences( projectOptions );

        if ( !this._referenceAssemblyLocators.TryGetValue( additionalPackageReferences, out var referenceAssemblyLocator ) )
        {
            // We lock instead of using ConcurrentDictionary because instantiating the class is expensive.
            lock ( this._sync )
            {
                if ( !this._referenceAssemblyLocators.TryGetValue( additionalPackageReferences, out referenceAssemblyLocator ) )
                {
                    referenceAssemblyLocator = new ReferenceAssemblyLocator( serviceProvider, additionalPackageReferences, this._tempFileManager );
                    this._referenceAssemblyLocators = this._referenceAssemblyLocators.Add( additionalPackageReferences, referenceAssemblyLocator );
                }
            }
        }

        return referenceAssemblyLocator;
    }
}
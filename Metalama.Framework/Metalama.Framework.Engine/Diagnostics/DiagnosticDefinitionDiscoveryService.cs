// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.Collections;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Metalama.Framework.Engine.Diagnostics;

public sealed class DiagnosticDefinitionDiscoveryService : IGlobalService
{
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly UserCodeInvoker _userCodeInvoker;

    static DiagnosticDefinitionDiscoveryService()
    {
        MetalamaEngineModuleInitializer.EnsureInitialized();
    }

    // This constructor is called in a path where no user code is involved
    public DiagnosticDefinitionDiscoveryService() : this(
        ServiceProvider<IProjectService>.Empty.WithService( new UserCodeInvoker( ServiceProvider<IGlobalService>.Empty ) ) ) { }

    internal DiagnosticDefinitionDiscoveryService( in ProjectServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider.Underlying;
        this._userCodeInvoker = serviceProvider.GetRequiredService<UserCodeInvoker>();
    }

    public IEnumerable<IDiagnosticDefinition> GetDiagnosticDefinitions( params Type[] types )
        => types.SelectAsReadOnlyList( this.GetDefinitions<IDiagnosticDefinition> ).SelectMany( d => d );

    internal IEnumerable<SuppressionDefinition> GetSuppressionDefinitions( params Type[] types )
        => types.SelectAsReadOnlyList( this.GetDefinitions<SuppressionDefinition> ).SelectMany( d => d );

    private IEnumerable<T> GetDefinitions<T>( Type declaringTypes )
        where T : class
    {
        T? GetFieldValue( FieldInfo f )
        {
            var executionContext = new UserCodeExecutionContext(
                this._serviceProvider,
                UserCodeDescription.Create( "getting the DiagnosticDefinition defined by field {0}", f ),
                diagnostics: NullDiagnosticAdder.Instance );

            if ( !this._userCodeInvoker.TryInvoke( () => f.GetValue( null ), executionContext, out var value ) )
            {
                return null;
            }

            return (T?) value;
        }

        T? GetPropertyValue( PropertyInfo p )
        {
            var executionContext = new UserCodeExecutionContext(
                this._serviceProvider,
                UserCodeDescription.Create( "getting the DiagnosticDefinition defined by property {0}", p ),
                diagnostics: NullDiagnosticAdder.Instance );

            if ( !this._userCodeInvoker.TryInvoke( () => p.GetValue( null ), executionContext, out var value ) )
            {
                return null;
            }

            return (T?) value;
        }

        return declaringTypes.GetFields( BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic )
            .Where( f => typeof(T).IsAssignableFrom( f.FieldType ) )
            .Select( GetFieldValue )
            .Concat(
                declaringTypes.GetProperties( BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic )
                    .Where( p => typeof(T).IsAssignableFrom( p.PropertyType ) )
                    .Select( GetPropertyValue ) )
            .WhereNotNull();
    }
}
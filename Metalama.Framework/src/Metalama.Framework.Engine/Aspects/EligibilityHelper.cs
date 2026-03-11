// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Eligibility.Implementation;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.UserCode;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Metalama.Framework.Engine.Aspects;

internal sealed partial class EligibilityHelper
{
    private readonly object _prototype;
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly object _requester;
    private readonly UserCodeInvoker _userCodeInvoker;
    private readonly List<KeyValuePair<Type, IEligibilityRule<IDeclaration>>> _eligibilityRules = new();

    private readonly ConcurrentDictionary<Type, Func<EligibilityHelper, IDiagnosticAdder, CompilationContext, bool>>
        _tryInitializeEligibilityMethods = new();

    public EligibilityHelper( object prototype, in ProjectServiceProvider serviceProvider, object requester )
    {
        this._prototype = prototype;
        this._serviceProvider = serviceProvider;
        this._requester = requester;
        this._userCodeInvoker = serviceProvider.GetRequiredService<UserCodeInvoker>();
    }

    private Func<EligibilityHelper, IDiagnosticAdder, CompilationContext, bool> GetTryInitializeEligibilityMethod( Type type )
        => this._tryInitializeEligibilityMethods.GetOrAdd( type, GetTryInitializeEligibilityMethodCore );

    private static Func<EligibilityHelper, IDiagnosticAdder, CompilationContext, bool> GetTryInitializeEligibilityMethodCore( Type type )
    {
        var method = typeof(EligibilityHelper).GetMethod( nameof(TryInitializeEligibility), BindingFlags.Instance | BindingFlags.NonPublic )
            .AssertNotNull()
            .MakeGenericMethod( type );

        var thisParameter = Expression.Parameter( typeof(EligibilityHelper) );
        var diagnosticAdderParameter = Expression.Parameter( typeof(IDiagnosticAdder) );
        var compilationContextParameter = Expression.Parameter( typeof(CompilationContext) );
        var callMethod = Expression.Call( thisParameter, method, diagnosticAdderParameter, compilationContextParameter );

        return Expression.Lambda<Func<EligibilityHelper, IDiagnosticAdder, CompilationContext, bool>>(
                callMethod,
                thisParameter,
                diagnosticAdderParameter,
                compilationContextParameter )
            .Compile();
    }

    private bool TryInitializeEligibility<T>( IDiagnosticAdder diagnosticAdder, CompilationContext compilationContext )
        where T : class, IDeclaration
    {
        if ( this._prototype is IEligible<T> eligible )
        {
            var builder = new EligibilityBuilder<T>();

            var executionContext = UserCodeExecutionContext.CreateInstance(
                this._serviceProvider,
                UserCodeDescription.Create( "executing BuildEligibility for {0}", this._requester ),
                compilationContext,
                diagnostics: diagnosticAdder );

            if ( !this._userCodeInvoker.TryInvoke( () => eligible.BuildEligibility( builder ), executionContext ) )
            {
                return false;
            }

            this._eligibilityRules.Add( new KeyValuePair<Type, IEligibilityRule<IDeclaration>>( typeof(T), ((IEligibilityBuilder<T>) builder).Build() ) );
        }

        return true;
    }

    public bool PopulateRules( IDiagnosticAdder diagnosticAdder, CompilationContext compilationContext )
    {
        var eligibilitySuccess = true;

        foreach ( var implementedInterface in this._prototype.GetType()
                     .GetInterfaces()
                     .Where( i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEligible<>) ) )
        {
            var declarationInterface = implementedInterface.GenericTypeArguments[0];

            // If methods are eligible, we need to check that the target method is not a local function or a lambda.
            if ( declarationInterface.IsAssignableFrom( typeof(IMethod) ) )
            {
                this._eligibilityRules.Add( new KeyValuePair<Type, IEligibilityRule<IDeclaration>>( typeof(IMethod), LocalFunctionEligibilityRule.Instance ) );
            }

            if ( declarationInterface.IsAssignableFrom( typeof(IParameter) ) )
            {
                this._eligibilityRules.Add(
                    new KeyValuePair<Type, IEligibilityRule<IDeclaration>>( typeof(IParameter), LocalFunctionParameterEligibilityRule.Instance ) );
            }

            eligibilitySuccess &= this.GetTryInitializeEligibilityMethod( declarationInterface ).Invoke( this, diagnosticAdder, compilationContext );
        }

        return eligibilitySuccess;
    }

    public void Add( Type type, IEligibilityRule<IDeclaration> eligibilityRule )
        => this._eligibilityRules.Add( new KeyValuePair<Type, IEligibilityRule<IDeclaration>>( type, eligibilityRule ) );

    public EligibleScenarios GetEligibility( IDeclaration obj, bool isInheritable )
    {
        if ( this._eligibilityRules.Count == 0 )
        {
            // Linker tests do not set this member but don't need to test eligibility.
            return EligibleScenarios.Default;
        }

        // We may execute user code, so we need to execute in a user context. This is not optimal, but we don't know,
        // in the current design, where we have user code. Also, we cannot report diagnostics in the current design,
        // so we have to let the exception fly.
        var executionContext = new UserCodeExecutionContext(
            this._serviceProvider,
            UserCodeDescription.Create( "evaluating eligibility for {0} applied to '{1}'", this._requester, obj ),
            obj.GetCompilationModel() );

        return this._userCodeInvoker.Invoke( GetEligibilityCore, executionContext );

        // Implementation, which all runs in a user context.
        EligibleScenarios GetEligibilityCore()
        {
            var declarationType = obj.GetType();
            var eligibility = EligibleScenarios.All;

            // If the aspect cannot be inherited, remove the inheritance eligibility.
            if ( isInheritable != true )
            {
                eligibility &= ~EligibleScenarios.Inheritance;
            }

            // Evaluate all eligibility rules that apply to the target declaration type.
            foreach ( var rule in this._eligibilityRules )
            {
                if ( rule.Key.IsAssignableFrom( declarationType ) )
                {
                    eligibility &= rule.Value.GetEligibility( obj );

                    if ( eligibility == EligibleScenarios.None )
                    {
                        return EligibleScenarios.None;
                    }
                }
            }

            return eligibility;
        }
    }

    public FormattableString? GetIneligibilityJustification( EligibleScenarios requestedEligibility, IDescribedObject<IDeclaration> describedObject )
    {
        var targetDeclaration = describedObject.Object;
        var declarationType = targetDeclaration.GetType();

        var group = new AndEligibilityRule<IDeclaration>(
            this._eligibilityRules.Where( r => r.Key.IsAssignableFrom( declarationType ) )
                .Select( r => r.Value )
                .ToImmutableArray() );

        // We may execute user code, so we need to execute in a user context. This is not optimal, but we don't know,
        // in the current design, where we have user code. Also, we cannot report diagnostics in the current design,
        // so we have to let the exception fly.
        var executionContext = new UserCodeExecutionContext(
            this._serviceProvider,
            UserCodeDescription.Create( "evaluating eligibility description for {0} applied to '{1}'", this, describedObject ),
            describedObject.Object.GetCompilationContext() );

        return this._userCodeInvoker.Invoke(
            () =>
                group.GetIneligibilityJustification(
                    requestedEligibility,
                    new DescribedObject<IDeclaration>( targetDeclaration, $"'{targetDeclaration}'" ) ),
            executionContext );
    }
}
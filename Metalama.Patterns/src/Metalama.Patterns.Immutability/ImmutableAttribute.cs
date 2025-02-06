// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Options;
using Metalama.Framework.Project;
using Metalama.Patterns.Immutability.Configuration;

namespace Metalama.Patterns.Immutability;

/// <summary>
/// An aspect that marks the target type as immutable (shallowly or deeply) and reports warnings
/// for mutable fields.
/// </summary>
[Inheritable]
public class ImmutableAttribute : TypeAspect, IHierarchicalOptionsProvider
{
    private readonly ImmutabilityKind _kind;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmutableAttribute"/> class.
    /// </summary>
    /// <param name="kind">The kind of immutability of the target type. The default value is <see cref="ImmutabilityKind.Shallow"/>.</param>
    public ImmutableAttribute( ImmutabilityKind kind = ImmutabilityKind.Shallow )
    {
        this._kind = kind;
    }

    IEnumerable<IHierarchicalOptions> IHierarchicalOptionsProvider.GetOptions( in OptionsProviderContext context )
        => new[] { new ImmutabilityOptions() { Kind = this._kind } };

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var field in builder.Target.Fields )
        {
            if ( field is { IsImplicitlyDeclared: false, IsStatic: false } )
            {
                if ( field is { Writeability: > Writeability.InitOnly } )
                {
                    builder.Diagnostics.Report( ImmutabilityDiagnostics.FieldMustBeReadOnly.WithArguments( field ), field );
                }
                else
                {
                    CheckDeepImmutability( field );
                }
            }
        }

        foreach ( var property in builder.Target.Properties )
        {
            if ( property is { IsAutoPropertyOrField: true, IsStatic: false } )
            {
                switch ( property.Writeability )
                {
                    case Writeability.All:
                        builder.Diagnostics.Report( ImmutabilityDiagnostics.PropertyMustHaveNoSetter.WithArguments( property ), property );

                        break;

                    case Writeability.None:
                        // Read-only properties are ignored.
                        continue;

                    default:
                        CheckDeepImmutability( property );

                        break;
                }
            }
        }

        void CheckDeepImmutability( IFieldOrProperty field )
        {
            if ( this._kind == ImmutabilityKind.Deep && !MetalamaExecutionContext.Current.ExecutionScenario.IsDesignTime )
            {
                builder.Outbound.Select( c => field.ForCompilation( c.Compilation ) )
                    .Where( field => field.Type.GetImmutabilityKind() != ImmutabilityKind.Deep )
                    .ReportDiagnostic( field => ImmutabilityDiagnostics.FieldOrPropertyMustBeOfDeeplyImmutableType.WithArguments( (field, field.DeclarationKind) ) );
            }
        }
    }
}
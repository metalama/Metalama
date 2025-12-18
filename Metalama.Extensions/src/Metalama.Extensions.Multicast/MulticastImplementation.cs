// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Extensions.Multicast;

/// <summary>
/// A reusable implementation of the multicasting logic. Multicast-enabled aspects can contain an instance of this class
/// and call its <see cref="BuildAspect{T}"/> method to perform multicasting.
/// </summary>
/// <remarks>
/// <para>
/// This class provides the core multicasting functionality that enables aspects to be applied at the assembly or type level
/// and automatically cascade to matching members based on filtering criteria defined through <see cref="IMulticastAttribute"/> properties.
/// </para>
/// <para>
/// When using this class directly (rather than deriving from <see cref="MulticastAspect"/>), your aspect must:
/// <list type="bullet">
/// <item>Implement <see cref="IMulticastAttribute"/> to expose the filtering properties.</item>
/// <item>Implement <see cref="IAspect{T}"/> for intermediate targets (<see cref="ICompilation"/> and <see cref="INamedType"/>) that
/// serve only as entry points for the multicasting process.</item>
/// <item>Implement <see cref="IAspect{T}"/> for final targets (e.g., <see cref="IMethod"/>, <see cref="IProperty"/>, <see cref="IField"/>)
/// where the aspect will actually perform its work.</item>
/// </list>
/// </para>
/// <para>
/// The multicasting process cascades from parent declarations to children: from assemblies to types, and from types to members.
/// At each level, the <see cref="BuildAspect{T}"/> method should be called to propagate the aspect to matching child declarations.
/// </para>
/// </remarks>
/// <seealso cref="MulticastAspect"/>
/// <seealso cref="IMulticastAttribute"/>
/// <seealso cref="MulticastTargets"/>
/// <seealso href="@migrating-multicasting"/>
[CompileTime]
public sealed class MulticastImplementation
{
    /// <summary>
    /// Gets the kind of declarations to which the aspect can be applied. This property is set from the class constructor.
    /// </summary>
    public MulticastTargets ConcreteTargets { get; }

    private readonly bool _multicastOnInheritance;

    /// <summary>
    /// Initializes a new instance of the <see cref="MulticastImplementation"/> class.
    /// </summary>
    /// <param name="concreteTargets">The set of targets to which concrete instances of the aspect can be applied. The aspect must implement the corresponding
    /// <see cref="IAspect{T}"/> generic interfaces.</param>
    /// <param name="multicastOnInheritance">A value indicating whether an aspect instance, when it is inherited, should also multicast to children. The default is <c>false</c>.
    /// It corresponds to the <see cref="MulticastInheritance.Strict"/> multicast inheritance mode in PostSharp. When set to <c>true</c>, the behavior is equivalent to <see cref="MulticastInheritance.Multicast"/>.
    /// </param>
    public MulticastImplementation( MulticastTargets concreteTargets, bool multicastOnInheritance = false )
    {
        this.ConcreteTargets = concreteTargets;
        this._multicastOnInheritance = multicastOnInheritance;
    }

    private bool MustMulticast( IAspectBuilder<IDeclaration> builder )
        => this._multicastOnInheritance || builder.AspectInstance.Predecessors[0].Kind != AspectPredecessorKind.Inherited;

    /// <summary>
    /// Implements the multicasting logic for a given declaration level. This method must be called from the
    /// <see cref="IAspect{T}.BuildAspect"/> method of the aspect class.
    /// </summary>
    /// <typeparam name="T">The type of the target declaration.</typeparam>
    /// <param name="builder">The <see cref="IAspectBuilder{T}"/> provided to the aspect's <c>BuildAspect</c> method.</param>
    /// <param name="implementConcreteAspect">An optional action that implements the aspect's actual behavior. This delegate is called
    /// only when the current target is a final (concrete) target as specified by <see cref="ConcreteTargets"/> and matches the
    /// filtering criteria. For intermediate targets (assemblies, types), pass <c>null</c> or omit this parameter.</param>
    /// <remarks>
    /// <para>
    /// This method performs two key operations:
    /// <list type="number">
    /// <item>If the current target is a concrete target (as defined by <see cref="ConcreteTargets"/>) and matches
    /// the filtering criteria defined by <see cref="IMulticastAttribute"/> properties, it invokes the
    /// <paramref name="implementConcreteAspect"/> delegate to apply the aspect's actual transformation.</item>
    /// <item>It propagates the aspect to matching child declarations based on the filtering criteria, enabling
    /// the cascading multicast behavior from assemblies to types to members.</item>
    /// </list>
    /// </para>
    /// <para>
    /// If the <see cref="IMulticastAttribute.AttributeExclude"/> property is <c>true</c> on the current aspect instance,
    /// no transformation is applied and the aspect is skipped.
    /// </para>
    /// </remarks>
    public void BuildAspect<T>( IAspectBuilder<T> builder, Action<IAspectBuilder<T>>? implementConcreteAspect = null )
        where T : class, IDeclaration
    {
        // Verifies the eligibility (implicitly reports an error if any).
        if ( !builder.VerifyEligibility( this.CreateEligibilityRule() ) )
        {
            return;
        }

        // Checks if there is anything to do anyway.
        var attributeGroup = new MulticastAttributeGroup( builder, this.ConcreteTargets );

        if ( attributeGroup.IsExcludeOnly )
        {
            builder.SkipAspect();

            return;
        }

        // Check if any concrete instance of the aspect should be implemented.
        if ( attributeGroup.IsMatch( builder.Target, MulticastTargetsHelper.GetMulticastTargets( builder.Target ) ) )
        {
            implementConcreteAspect?.Invoke( builder );
        }

        // Multicast to children.
        if ( !builder.IsAspectSkipped )
        {
            if ( !this.MustMulticast( builder ) )
            {
                return;
            }

            switch ( builder )
            {
                case IAspectBuilder<ICompilation> compilationAspectBuilder:
                    this.AddChildAspects( compilationAspectBuilder, attributeGroup );

                    break;

                case IAspectBuilder<IMethod> methodAspectBuilder:
                    AddChildAspects( methodAspectBuilder, attributeGroup );

                    break;

                case IAspectBuilder<IHasAccessors> propertyOrEventAspectBuilder:

                    AddChildAspects( propertyOrEventAspectBuilder, attributeGroup );

                    break;

                case IAspectBuilder<INamedType> namedTypeAspectBuilder:
                    AddChildAspects( namedTypeAspectBuilder, attributeGroup );

                    break;
            }
        }
    }

    private static bool Filter( IDeclaration declaration, MulticastAttributeGroup attributeGroup, MulticastTargets targets )
        => attributeGroup.IsMatch( declaration, targets );

    private static bool FilterDeclaringType( INamedType type, MulticastAttributeGroup attributeGroup, MulticastTargets targets )
        => attributeGroup.IsMatch( type, targets ) && type.IsAspectEligible( attributeGroup.AspectClass.Type );

    private bool FilterType( INamedType type, MulticastAttributeGroup attributeGroup, MulticastTargets targets )
        => this.MatchesTypeKind( type, targets ) && attributeGroup.IsMatch( type, targets );

    private bool MatchesTypeKind( INamedType namedType, MulticastTargets targets )
    {
        var resultingTargets = targets == 0 ? this.ConcreteTargets : targets & this.ConcreteTargets;

        return namedType.TypeKind switch
        {
            TypeKind.Class => resultingTargets.HasFlagFast( MulticastTargets.Class ),
            TypeKind.Struct => resultingTargets.HasFlagFast( MulticastTargets.Struct ),
            TypeKind.Interface => resultingTargets.HasFlagFast( MulticastTargets.Interface ),
            TypeKind.Delegate => resultingTargets.HasFlagFast( MulticastTargets.Delegate ),
            TypeKind.Enum => resultingTargets.HasFlagFast( MulticastTargets.Enum ),
            _ => false
        };
    }

    private IEligibilityRule<IDeclaration> CreateEligibilityRule()
    {
        List<Action<IEligibilityBuilder<IDeclaration>>> rules = [];

        void AcceptAssembly()
        {
            rules.Add( builder => builder.MustBeInstanceOfType( typeof(ICompilation) ) );
        }

        void AcceptClassOrStruct()
        {
            rules.Add(
                builder => builder.Convert()
                    .To<INamedType>()
                    .MustSatisfy(
                        t => t.TypeKind is TypeKind.Class or TypeKind.Struct,
                        t => $"{t} is neither a class, struct or record" ) );
        }

        void AcceptHasAccessor()
        {
            rules.Add( builder => builder.MustBeInstanceOfType( typeof(IHasAccessors) ) );
        }

        void AcceptHasParametersOrAncestor()
        {
            AcceptAssembly();
            AcceptClassOrStruct();
            AcceptHasAccessor();
            rules.Add( builder => builder.MustBeInstanceOfType( typeof(IHasParameters) ) );
        }

        if ( this.ConcreteTargets.HasFlagFast( MulticastTargets.Assembly ) )
        {
            AcceptAssembly();
        }

        if ( this.ConcreteTargets.HasFlagFast( MulticastTargets.Class ) )
        {
            AcceptAssembly();

            rules.Add(
                builder => builder.Convert()
                    .To<INamedType>()
                    .MustSatisfy( t => t.TypeKind is TypeKind.Class, t => $"{t} is not a class or record class" ) );
        }

        if ( this.ConcreteTargets.HasFlagFast( MulticastTargets.Struct ) )
        {
            AcceptAssembly();

            rules.Add(
                builder => builder.Convert()
                    .To<INamedType>()
                    .MustSatisfy( t => t.TypeKind is TypeKind.Struct, t => $"{t} is not a struct or record struct" ) );
        }

        if ( this.ConcreteTargets.HasFlagFast( MulticastTargets.Method ) )
        {
            AcceptAssembly();
            AcceptClassOrStruct();
            AcceptHasAccessor();

            rules.Add( builder => builder.MustBeInstanceOfType( typeof(IMethod) ) );
        }

        if ( this.ConcreteTargets.HasFlagFast( MulticastTargets.InstanceConstructor ) )
        {
            AcceptAssembly();
            AcceptClassOrStruct();
            rules.Add( builder => builder.Convert().To<IConstructor>().MustNotBeStatic() );
        }

        if ( this.ConcreteTargets.HasFlagFast( MulticastTargets.StaticConstructor ) )
        {
            AcceptAssembly();
            AcceptClassOrStruct();

            rules.Add( builder => builder.Convert().To<IConstructor>().MustBeStatic() );
        }

        if ( this.ConcreteTargets.HasFlagFast( MulticastTargets.Property ) )
        {
            AcceptAssembly();
            AcceptClassOrStruct();

            rules.Add(
                builder =>
                {
                    builder.MustBeExplicitlyDeclared();
                    builder.MustBeInstanceOfType( typeof(IProperty) );
                } );
        }

        if ( this.ConcreteTargets.HasFlagFast( MulticastTargets.Event ) )
        {
            AcceptAssembly();
            AcceptClassOrStruct();

            rules.Add( builder => builder.MustBeInstanceOfType( typeof(IEvent) ) );
        }

        if ( this.ConcreteTargets.HasFlagFast( MulticastTargets.Field ) )
        {
            AcceptAssembly();
            AcceptClassOrStruct();

            rules.Add(
                builder =>
                {
                    builder.MustBeExplicitlyDeclared();
                    builder.MustBeInstanceOfType( typeof(IField) );
                } );
        }

        if ( this.ConcreteTargets.HasFlagFast( MulticastTargets.Parameter ) )
        {
            AcceptHasParametersOrAncestor();

            rules.Add(
                builder =>
                {
                    var parameterEligibility = builder.Convert().To<IParameter>();
                    parameterEligibility.DeclaringMember().MustBeExplicitlyDeclared();
                    parameterEligibility.MustSatisfy( p => !p.IsReturnParameter, p => $"{p} is the return parameter" );
                } );
        }

        if ( this.ConcreteTargets.HasFlagFast( MulticastTargets.ReturnValue ) )
        {
            AcceptHasParametersOrAncestor();

            rules.Add(
                builder =>
                {
                    var parameterEligibility = builder.Convert().To<IParameter>();
                    parameterEligibility.DeclaringMember().MustBeExplicitlyDeclared();
                    parameterEligibility.MustSatisfy( p => !p.IsReturnParameter, p => $"{p} is not the return parameter" );
                } );
        }

        return EligibilityRuleFactory.CreateRule<IDeclaration>( builder => builder.MustSatisfyAny( rules.ToArray() ) );
    }

    private void AddChildAspects( IAspectBuilder<ICompilation> builder, MulticastAttributeGroup attributeGroup )
    {
        var implementation = this;

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.AnyType ) )
        {
            builder.Outbound
                .SelectMany( c => c.AllTypes.Where( t => implementation.FilterType( t, attributeGroup, MulticastTargets.AnyType ) ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.StaticConstructor ) )
        {
            builder.Outbound
                .SelectMany(
                    c => c.AllTypes.Where( t => FilterDeclaringType( t, attributeGroup, MulticastTargets.StaticConstructor ) && t.StaticConstructor != null )
                        .Select( t => t.StaticConstructor! ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.InstanceConstructor ) )
        {
            builder.Outbound
                .SelectMany(
                    compilation => compilation.AllTypes
                        .Where( t => FilterDeclaringType( t, attributeGroup, MulticastTargets.InstanceConstructor ) )
                        .SelectMany( t => t.Constructors.Where( c => (!c.IsImplicitlyDeclared || c.Parameters.Count == 0) && Filter( c, attributeGroup, MulticastTargets.InstanceConstructor ) ) ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.Method ) )
        {
            builder
                .Outbound.SelectMany(
                    c => c.AllTypes
                        .Where( t => FilterDeclaringType( t, attributeGroup, MulticastTargets.Method ) )
                        .SelectMany(
                            t => t.MethodsAndAccessors().Where( m => !m.IsImplicitlyDeclared && Filter( m, attributeGroup, MulticastTargets.Method ) ) ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.Parameter ) )
        {
            builder
                .Outbound.SelectMany(
                    c => c.AllTypes
                        .Where( t => FilterDeclaringType( t, attributeGroup, MulticastTargets.Method ) )
                        .SelectMany(
                            t => t.MethodsAndAccessors().Where( m => !m.IsImplicitlyDeclared && Filter( m, attributeGroup, MulticastTargets.Method ) ) )
                        .SelectMany( m => m.Parameters.Where( p => Filter( p, attributeGroup, MulticastTargets.Method ) ) ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.ReturnValue ) )
        {
            builder
                .Outbound.SelectMany(
                    c => c.AllTypes
                        .Where( t => FilterDeclaringType( t, attributeGroup, MulticastTargets.ReturnValue ) )
                        .SelectMany(
                            t => t.MethodsAndAccessors()
                                .Where(
                                    m => !m.IsImplicitlyDeclared && Filter( m, attributeGroup, MulticastTargets.ReturnValue )
                                                                 && !m.ReturnType.Equals( SpecialType.Void ) ) )
                        .Select( m => m.ReturnParameter ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.Field ) )
        {
            builder
                .Outbound.SelectMany(
                    c => c.AllTypes.Where( t => FilterDeclaringType( t, attributeGroup, MulticastTargets.Field ) )
                        .SelectMany( t => t.Fields.Where( f => !f.IsImplicitlyDeclared && Filter( f, attributeGroup, MulticastTargets.Field ) ) ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.Property ) )
        {
            builder
                .Outbound.SelectMany(
                    c => c.AllTypes.Where( t => FilterDeclaringType( t, attributeGroup, MulticastTargets.Property ) )
                        .SelectMany( t => t.Properties.Where( p => !p.IsImplicitlyDeclared && Filter( p, attributeGroup, MulticastTargets.Property ) ) ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.Event ) )
        {
            builder
                .Outbound.SelectMany(
                    c => c.AllTypes.Where( t => FilterDeclaringType( t, attributeGroup, MulticastTargets.Event ) )
                        .SelectMany( t => t.Events.Where( e => !e.IsImplicitlyDeclared && Filter( e, attributeGroup, MulticastTargets.Event ) ) ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }
    }

    private static void AddChildAspects( IAspectBuilder<INamedType> builder, MulticastAttributeGroup attributeGroup )
    {
        // Multicast to children.
        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.StaticConstructor ) )
        {
            builder
                .Outbound.SelectMany( t => t.StaticConstructor != null ? [t.StaticConstructor] : Enumerable.Empty<IConstructor>() )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.InstanceConstructor ) )
        {
            builder
                .Outbound.SelectMany( t => t.Constructors.Where( c => (!c.IsImplicitlyDeclared || c.Parameters.Count == 0) && Filter( c, attributeGroup, MulticastTargets.InstanceConstructor ) ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.Method ) )
        {
            builder
                .Outbound.SelectMany(
                    t => t.MethodsAndAccessors().Where( m => !m.IsImplicitlyDeclared && Filter( m, attributeGroup, MulticastTargets.Method ) ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.Parameter ) )
        {
            builder
                .Outbound.SelectMany(
                    t => t.MethodsAndAccessors()
                        .Where( m => !m.IsImplicitlyDeclared && Filter( m, attributeGroup, MulticastTargets.Parameter ) )
                        .SelectMany( m => m.Parameters.Where( p => !p.IsImplicitlyDeclared && Filter( p, attributeGroup, MulticastTargets.Parameter ) ) ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.ReturnValue ) )
        {
            builder
                .Outbound.SelectMany(
                    t => t.MethodsAndAccessors()
                        .Where(
                            m => !m.IsImplicitlyDeclared && Filter( m, attributeGroup, MulticastTargets.ReturnValue )
                                                         && !m.ReturnType.Equals( SpecialType.Void ) )
                        .Select( m => m.ReturnParameter ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.Field ) )
        {
            builder
                .Outbound.SelectMany( t => t.Fields.Where( f => !f.IsImplicitlyDeclared && Filter( f, attributeGroup, MulticastTargets.Field ) ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.Property ) )
        {
            builder
                .Outbound.SelectMany( t => t.Properties.Where( p => !p.IsImplicitlyDeclared && Filter( p, attributeGroup, MulticastTargets.Property ) ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.Event ) )
        {
            builder
                .Outbound.SelectMany( t => t.Properties.Where( p => !p.IsImplicitlyDeclared && Filter( p, attributeGroup, MulticastTargets.Event ) ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }
    }

    private static void AddChildAspects( IAspectBuilder<IHasAccessors> builder, MulticastAttributeGroup attributeGroup )
    {
        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.Method ) )
        {
            builder
                .Outbound.SelectMany( t => t.Accessors.Where( m => !m.IsImplicitlyDeclared && Filter( m, attributeGroup, MulticastTargets.Method ) ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.Parameter ) )
        {
            builder
                .Outbound.SelectMany(
                    t => t.Accessors
                        .Where( m => !m.IsImplicitlyDeclared && Filter( m, attributeGroup, MulticastTargets.Parameter ) )
                        .SelectMany( m => m.Parameters.Where( p => !p.IsImplicitlyDeclared && Filter( p, attributeGroup, MulticastTargets.Parameter ) ) ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.ReturnValue ) )
        {
            builder
                .Outbound.SelectMany(
                    t => t.Accessors
                        .Where(
                            m => !m.IsImplicitlyDeclared && Filter( m, attributeGroup, MulticastTargets.ReturnValue )
                                                         && !m.ReturnType.Equals( SpecialType.Void ) )
                        .Select( m => m.ReturnParameter ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }
    }

    private static void AddChildAspects( IAspectBuilder<IMethod> builder, MulticastAttributeGroup attributeGroup )
    {
        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.Parameter ) )
        {
            builder
                .Outbound.SelectMany( m => m.Parameters.Where( p => !p.IsImplicitlyDeclared && Filter( p, attributeGroup, MulticastTargets.Method ) ) )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }

        if ( attributeGroup.TargetsAnyDeclarationKind( MulticastTargets.ReturnValue ) )
        {
            builder
                .Outbound.SelectMany( m => m.ReturnParameter.Type.Equals( SpecialType.Void ) ? Enumerable.Empty<IParameter>() : [m.ReturnParameter] )
                .AddAspectIfEligible( attributeGroup.AspectClass.Type, attributeGroup.GetMatchingAspect );
        }
    }
}
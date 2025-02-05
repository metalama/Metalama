// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.DependencyInjection.Implementation;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Options;
using System;

namespace Metalama.Extensions.DependencyInjection;

/// <summary>
/// Specifications of an introduced dependency. 
/// </summary>
[CompileTime]
public record DependencyProperties
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyProperties"/> class. This overload excepts the dependency type as an <see cref="IType"/>.
    /// </summary>
    /// <param name="targetType">The type into which the dependency should be introduced.</param>
    /// <param name="dependencyType">The type of the dependency.</param>
    /// <param name="name">The name of the field or property that should expose the property.</param>
    /// <param name="isStatic">Indicates whether the dependency field or property should be static.</param>
    /// <param name="isRequired">Indicates whether the dependency is required. When this parameter is set to <c>false</c>, the code will accept missing dependencies.</param>
    /// <param name="isLazy">Indicates whether the dependency should be lazily resolved upon first use. Whe this parameter is set to <c>false</c>, the dependency is resolved upon object construction.</param>
    /// <param name="kind">Either <see cref="DeclarationKind.Property"/> or <see cref="DeclarationKind.Field"/>.</param>
    public DependencyProperties(
        INamedType targetType,
        IType dependencyType,
        string name,
        bool isStatic = false,
        bool? isRequired = null,
        bool? isLazy = null,
        DeclarationKind kind = DeclarationKind.Field )
    {
        if ( kind is not (DeclarationKind.Property or DeclarationKind.Field) )
        {
            throw new ArgumentOutOfRangeException( nameof(kind) );
        }

        this.TargetType = targetType;
        this.DependencyType = dependencyType;
        this.Name = name;
        this.IsStatic = isStatic;
        this.Kind = kind;

        this.Options = this.TargetType.Enhancements()
            .GetOptions<DependencyInjectionOptions>()
            .ApplyChangesSafe( new DependencyInjectionOptions { IsLazy = isLazy, IsRequired = isRequired }, default )!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyProperties"/> class. This overload excepts the dependency type as a <see cref="Type"/>.
    /// </summary>
    /// <param name="targetType">The type into which the dependency should be introduced.</param>
    /// <param name="dependencyType">The type of the dependency.</param>
    /// <param name="name">The name of the field or property that should expose the property.</param>
    /// <param name="isStatic">Indicates whether the dependency field or property should be static.</param>
    /// <param name="isRequired">Indicates whether the dependency is required. When this parameter is set to <c>false</c>, the code will accept missing dependencies.</param>
    /// <param name="isLazy">Indicates whether the dependency should be lazily resolved upon first use. Whe this parameter is set to <c>false</c>, the dependency is resolved upon object construction.</param>
    /// <param name="kind">Either <see cref="DeclarationKind.Property"/> or <see cref="DeclarationKind.Field"/>.</param>
    public DependencyProperties(
        INamedType targetType,
        Type dependencyType,
        string name,
        bool isStatic = false,
        bool? isRequired = null,
        bool? isLazy = null,
        DeclarationKind kind = DeclarationKind.Field ) : this( targetType, TypeFactory.GetType( dependencyType ), name, isStatic, isRequired, isLazy, kind ) { }

    /// <summary>
    /// Gets a value indicating whether the dependency is required. When this property is set to <c>false</c>, the code will accept missing dependencies.
    /// The default value, when this property is neither specified nor overwritten, is <c>true</c>. 
    /// </summary>
    public bool IsRequired => this.Options.IsRequired!.Value;

    /// <summary>
    /// Gets a value indicating whether the dependency should be resolved lazily upon first use.
    /// The default value, when this property is neither specified nor overwritten, is <c>false</c>.  
    /// </summary>
    public bool IsLazy => this.Options.IsLazy!.Value;

    /// <summary>
    /// Gets the options set by the options framework.
    /// </summary>
    public DependencyInjectionOptions Options { get; }

    /// <summary>
    /// Gets the type into which the dependency should be injected.
    /// </summary>
    public INamedType TargetType { get; }

    /// <summary>
    /// Gets the dependency type.
    /// </summary>
    public IType DependencyType { get; }

    /// <summary>
    /// Gets the name of the field or property that should expose the dependency.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets a value indicating whether the dependency field or property is static.
    /// </summary>
    public bool IsStatic { get; }

    /// <summary>
    /// Gets the kind of declaration to introduce i.e. <see cref="DeclarationKind.Field"/> or <see cref="DeclarationKind.Property"/>.
    /// </summary>
    public DeclarationKind Kind { get; }
}
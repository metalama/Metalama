// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Project;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Extensions.DependencyInjection;

/// <summary>
/// Extends the <see cref="IProject"/> and <see cref="IAspectBuilder"/> interfaces.
/// </summary>
[CompileTime]
[PublicAPI]
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Configures <c>Metalama.Extensions.DependencyInjection</c> for the current project.
    /// </summary>
    /// <param name="receiver">The <see cref="IAspectReceiver{TDeclaration}"/> for current compilation.</param>
    /// <param name="configure">A delegate that configures the framework.</param>
    public static void ConfigureDependencyInjection(
        this IAspectReceiver<ICompilation> receiver,
        Action<DependencyInjectionOptionsBuilder> configure )
    {
        var builder = new DependencyInjectionOptionsBuilder();
        configure( builder );

        var options = builder.Build();

        receiver.SetOptions( options );
    }

    /// <summary>
    /// Configures <c>Metalama.Extensions.DependencyInjection</c> for a given type.
    /// </summary>
    /// <param name="receiver">The <see cref="IAspectReceiver{TDeclaration}"/> for the type.</param>
    /// <param name="configure">A delegate that configures the framework.</param>
    public static void ConfigureDependencyInjection(
        this IAspectReceiver<INamedType> receiver,
        Action<DependencyInjectionOptionsBuilder> configure )
    {
        var builder = new DependencyInjectionOptionsBuilder();
        configure( builder );

        var options = builder.Build();

        receiver.SetOptions( options );
    }

    /// <summary>
    /// Configures <c>Metalama.Extensions.DependencyInjection</c> for a given type.
    /// </summary>
    /// <param name="receiver">The <see cref="IAspectReceiver{TDeclaration}"/> for the type.</param>
    /// <param name="configure">A delegate that configures the framework.</param>
    public static void ConfigureDependencyInjection(
        this IAspectReceiver<INamespace> receiver,
        Action<DependencyInjectionOptionsBuilder> configure )
    {
        var builder = new DependencyInjectionOptionsBuilder();
        configure( builder );

        var options = builder.Build();

        receiver.SetOptions( options );
    }

    /// <summary>
    /// Tries to introduce a dependency into a specified type. 
    /// </summary>
    /// <param name="aspectBuilder">An <see cref="IAspectBuilder"/>.</param>
    /// <param name="properties">The properties of the dependency to introduce.</param>
    /// <param name="dependencyFieldOrProperty">When the method succeeds, the field or dependency that represents the dependency.</param>
    /// <returns><c>true</c> in case of success, otherwise <c>false</c>. When the field or property already exists, this method returns <c>true</c> and has no effect.</returns>
    [Obsolete( "Use the IntroduceDependency method instead." )]
    public static bool TryIntroduceDependency(
        this IAspectBuilder aspectBuilder,
        DependencyProperties properties,
        [NotNullWhen( true )] out IFieldOrProperty? dependencyFieldOrProperty )
    {
        if ( !properties.Options.TryGetFramework( properties, aspectBuilder.Diagnostics, out var framework ) )
        {
            aspectBuilder.SkipAspect();

            dependencyFieldOrProperty = null;

            return false;
        }

        var result = framework.IntroduceDependency( properties, aspectBuilder.With( properties.TargetType ) );

        if ( result.Outcome is AdviceOutcome.Default or AdviceOutcome.Ignore )
        {
            dependencyFieldOrProperty = result.Declaration;

            return true;
        }
        else
        {
            dependencyFieldOrProperty = null;

            return false;
        }
    }

    public static IntroduceDependencyResult IntroduceDependency(
        this IAdviser<INamedType> adviser,
        IType dependencyType,
        DependencyOptions? options = null )
    {
        options ??= DependencyOptions.Default;
        var name = options.MemberName ?? GetMemberName();

        var properties = new DependencyProperties(
            adviser.Target,
            dependencyType,
            name,
            options.IsStatic,
            options.IsRequired,
            options.IsLazy,
            options.MemberKind );

        if ( !properties.Options.TryGetFramework( properties, adviser.Diagnostics, out var framework ) )
        {
            return IntroduceDependencyResult.Error;
        }

        return framework.IntroduceDependency( properties, adviser.With( properties.TargetType ) );

        string GetMemberName()
        {
            var baseName = dependencyType switch
            {
                INamedType namedType => namedType.Name,
                _ => throw new ArgumentOutOfRangeException( nameof(options), $"Cannot generate member name from type '{dependencyType}'." )
            };

            if ( dependencyType.TypeKind == TypeKind.Interface && baseName[0] == 'I' )
            {
                baseName = baseName[1..];
            }

            if ( options.MemberKind == DeclarationKind.Field )
            {
                // We take this default naming convention for backward compatibility with Metalama.Patterns.
                // TODO: Use naming conventions from .editorconfig.
                return "_" + baseName[0].ToString().ToLowerInvariant() + baseName[1..];
            }
            else
            {
                return baseName;
            }
        }
    }

    public static IntroduceDependencyResult IntroduceDependency(
        this IAdviser<INamedType> adviser,
        Type dependencyType,
        DependencyOptions? options = null )
        => adviser.IntroduceDependency(
            adviser.Target.Compilation.Factory.GetTypeByReflectionType( dependencyType ),
            options );
}
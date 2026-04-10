// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.DeclarationBuilders
{
    /// <summary>
    /// Allows you to configure a constructor that has been created by an advice.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this builder when introducing constructors via <see cref="AdviserExtensions.IntroduceConstructor"/>.
    /// </para>
    /// <para>
    /// The builder allows you to configure the constructor initializer (base or this call) and add arguments
    /// to pass to the chained constructor.
    /// </para>
    /// </remarks>
    /// <seealso cref="IConstructor"/>
    /// <seealso cref="IMethodBaseBuilder"/>
    /// <seealso cref="AdviserExtensions.IntroduceConstructor"/>
    /// <seealso cref="AdviserExtensions.IntroduceParameter(IAdviser{IConstructor}, string, IType, Advising.IPullStrategy?, System.Collections.Immutable.ImmutableArray{AttributeConstruction}, Advising.IConstructorOverloadingStrategy?)"/>
    /// <seealso cref="AdviserExtensions.AddInitializer(IAdviser{IConstructor}, string, object?, object?)"/>
    /// <seealso href="@introducing-constructor-parameters"/>
    /// <seealso href="@initializers"/>
    public interface IConstructorBuilder : IConstructor, IMethodBaseBuilder
    {
        /// <summary>
        /// Gets or sets the kind of constructor initializer (e.g., <see cref="ConstructorInitializerKind.Base"/> or <see cref="ConstructorInitializerKind.This"/>).
        /// </summary>
        new ConstructorInitializerKind InitializerKind { get; set; }

        /// <summary>
        /// Adds an argument to the constructor initializer (base or this call).
        /// </summary>
        /// <param name="initializerArgumentExpression">The expression to pass as an argument to the constructor initializer.</param>
        /// <param name="parameterName">The optional name of the parameter to which the argument should be assigned. If <c>null</c>, arguments are assigned positionally.</param>
        /// <remarks>
        /// This method is commonly used when introducing constructor parameters to pass the new parameter value
        /// to base or chained constructors via the <c>: base(...)</c> or <c>: this(...)</c> syntax.
        /// </remarks>
        /// <seealso href="@introducing-constructor-parameters"/>
        void AddInitializerArgument( IExpression initializerArgumentExpression, string? parameterName = null );
    }
}
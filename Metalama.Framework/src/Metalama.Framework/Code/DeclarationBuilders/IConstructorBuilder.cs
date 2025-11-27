// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code.DeclarationBuilders
{
    /// <summary>
    /// Allows you to configure a constructor that has been created by an advice.
    /// </summary>
    /// <seealso cref="IConstructor"/>
    /// <seealso cref="IMethodBaseBuilder"/>
    /// <seealso cref="AdviserExtensions.IntroduceConstructor"/>
    /// <seealso href="@introducing-constructor-parameters"/>
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
        void AddInitializerArgument( IExpression initializerArgumentExpression, string? parameterName = null );
    }
}
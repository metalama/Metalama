// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Diagnostics;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Extension methods for the <see cref="IAttribute"/> interface.
    /// </summary>
    /// <seealso cref="IAttribute"/>
    /// <seealso cref="AttributeConstruction"/>
    [PublicAPI]
    public static class AttributeExtensions
    {
        /// <summary>
        /// Converts an <see cref="IAttribute"/> to an <see cref="AttributeConstruction"/> object.
        /// </summary>
        /// <param name="attribute">The attribute to convert.</param>
        /// <returns>An <see cref="AttributeConstruction"/> representing the attribute.</returns>
        public static AttributeConstruction ToAttributeConstruction( this IAttribute attribute )
            => AttributeConstruction.Create( attribute.Constructor, attribute.ConstructorArguments, attribute.NamedArguments );

        /// <summary>
        /// Tries to get a named argument (i.e. the value assigned to a field or property).
        /// </summary>
        /// <param name="attribute">The attribute to query.</param>
        /// <param name="name">The name of the named argument.</param>
        /// <param name="value">When this method returns <c>true</c>, contains the value of the named argument; otherwise, the default value.</param>
        /// <returns><c>true</c> if the attribute defines this named argument, otherwise <c>false</c>.</returns>
        public static bool TryGetNamedArgument( this IAttribute attribute, string name, out TypedConstant value )
        {
            foreach ( var argument in attribute.NamedArguments )
            {
                if ( argument.Key == name )
                {
                    value = argument.Value;

                    return true;
                }
            }

            value = default;

            return false;
        }

        /// <summary>
        /// Tries to gets the value of an argument given its name, considering both <see cref="IAttributeData.NamedArguments"/> and <see cref="IAttributeData.ConstructorArguments"/>.
        /// For constructor arguments, the name of the corresponding parameter is taken into account. Comparisons are case-insensitive.
        /// In case of ambiguity, the first match wins.
        /// </summary>
        /// <typeparam name="T">The expected type of the argument value.</typeparam>
        /// <param name="attribute">The attribute to query.</param>
        /// <param name="name">The name of the argument.</param>
        /// <param name="defaultValue">The value to return if the argument is not found.</param>
        /// <returns>The value of the argument, or <paramref name="defaultValue"/> if not found.</returns>
        public static T? GetArgumentValue<T>( this IAttribute attribute, string name, T? defaultValue = default )
        {
            if ( attribute.TryGetArgumentValue( name, out T? value ) )
            {
                return value;
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Tries to gets the value of an argument given its name, considering both <see cref="IAttributeData.NamedArguments"/> and <see cref="IAttributeData.ConstructorArguments"/>.
        /// For constructor arguments, the name of the corresponding parameter is taken into account. Comparisons are case-insensitive.
        /// In case of ambiguity, the first match wins.
        /// </summary>
        /// <typeparam name="T">The expected type of the argument value.</typeparam>
        /// <param name="attribute">The attribute to query.</param>
        /// <param name="name">The name of the argument.</param>
        /// <param name="value">When this method returns <c>true</c>, contains the value of the argument; otherwise, the default value.</param>
        /// <returns><c>true</c> if the argument was found; otherwise, <c>false</c>.</returns>
        public static bool TryGetArgumentValue<T>( this IAttribute attribute, string name, [MaybeNullWhen( false )] out T value )
        {
            foreach ( var argument in attribute.NamedArguments )
            {
                if ( string.Equals( argument.Key, name, StringComparison.OrdinalIgnoreCase ) )
                {
                    value = (T) argument.Value.Value!;

                    return true;
                }
            }

            for ( var index = 0; index < attribute.ConstructorArguments.Length; index++ )
            {
                var parameter = attribute.Constructor.Parameters[index];

                if ( string.Equals( parameter.Name, name, StringComparison.OrdinalIgnoreCase ) )
                {
                    var argument = attribute.ConstructorArguments[index];

                    value = (T) argument.Value!;

                    return true;
                }
            }

            value = default;

            return false;
        }

        /// <summary>
        /// Tries to construct an instance of the attribute represented by the current <see cref="IAttribute"/>. The attribute type
        /// must not be a run-time-only type.
        /// </summary>
        /// <param name="attribute">The attribute to construct.</param>
        /// <param name="diagnosticSink">A sink for reporting diagnostics during construction.</param>
        /// <param name="constructedAttribute">When this method returns <c>true</c>, contains the constructed attribute instance; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the attribute was successfully constructed; otherwise, <c>false</c>.</returns>
        public static bool TryConstruct(
            this IAttribute attribute,
            ScopedDiagnosticSink diagnosticSink,
            [NotNullWhen( true )] out Attribute? constructedAttribute )
        {
            return ((ICompilationInternal) attribute.Compilation).Helpers.TryConstructAttribute( attribute, diagnosticSink, out constructedAttribute );
        }

        /// <summary>
        /// Tries to construct an instance of the attribute represented by the current <see cref="IAttribute"/>. The attribute type
        /// must not be a run-time-only type.
        /// </summary>
        /// <param name="attribute">The attribute to construct.</param>
        /// <param name="constructedAttribute">When this method returns <c>true</c>, contains the constructed attribute instance; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the attribute was successfully constructed; otherwise, <c>false</c>.</returns>
        public static bool TryConstruct(
            this IAttribute attribute,
            [NotNullWhen( true )] out Attribute? constructedAttribute )
        {
            return ((ICompilationInternal) attribute.Compilation).Helpers.TryConstructAttribute( attribute, default, out constructedAttribute );
        }

        /// <summary>
        /// Constructs an instance of the attribute represented by the current <see cref="IAttribute"/>. The attribute type must not be a run-time-only type.
        /// </summary>
        /// <param name="attribute">The attribute to construct.</param>
        /// <returns>The constructed attribute instance.</returns>
        public static Attribute Construct( this IAttribute attribute )
            => ((ICompilationInternal) attribute.Compilation).Helpers.ConstructAttribute( attribute );

        /// <summary>
        /// Constructs an instance of the attribute represented by the current <see cref="IAttribute"/>. The attribute type must not be a run-time-only type.
        /// </summary>
        /// <typeparam name="T">The expected attribute type.</typeparam>
        /// <param name="attribute">The attribute to construct.</param>
        /// <returns>The constructed attribute instance.</returns>
        public static T Construct<T>( this IAttribute attribute )
            where T : Attribute
            => (T) ((ICompilationInternal) attribute.Compilation).Helpers.ConstructAttribute( attribute );

        /// <summary>
        /// Tries to construct a strongly-typed instance of the attribute represented by the current <see cref="IAttribute"/>. The attribute type
        /// must not be a run-time-only type.
        /// </summary>
        /// <typeparam name="T">The expected attribute type.</typeparam>
        /// <param name="attribute">The attribute to construct.</param>
        /// <param name="constructedAttribute">When this method returns <c>true</c>, contains the constructed attribute instance; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the attribute was successfully constructed; otherwise, <c>false</c>.</returns>
        public static bool TryConstruct<T>(
            this IAttribute attribute,
            [NotNullWhen( true )] out T? constructedAttribute )
            where T : Attribute
        {
            if ( ((ICompilationInternal) attribute.Compilation).Helpers.TryConstructAttribute( attribute, default, out var result ) )
            {
                constructedAttribute = (T) result;

                return true;
            }

            constructedAttribute = default;

            return false;
        }

        internal static object? GetNamedArgumentValue( this IAttribute attribute, string name )
        {
            if ( attribute.TryGetNamedArgument( name, out var value ) )
            {
                return value.Value;
            }
            else
            {
                return null;
            }
        }
    }
}

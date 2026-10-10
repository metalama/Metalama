// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using Microsoft.CodeAnalysis;
using static Metalama.Framework.Diagnostics.Severity;

#pragma warning disable SA1118 // Allow multi-line parameters.

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    public static class SerializationDiagnosticDescriptors
    {
        // Reserved range 200-219

        private const string _category = "Metalama.Serialization";

        internal const string UnsupportedSerializationMessage =
            "A compile-time value cannot be converted to a run-time value because the type '{0}' is not supported. Use a supported type or a " +
            "type that implements the IExpressionBuilder interface.";

        internal static readonly DiagnosticDefinition<object> UnsupportedSerialization = new(
            "LAMA0200",
            _category,
            UnsupportedSerializationMessage,
            Error,
            "Compile-time type not serializable." );

        internal static readonly DiagnosticDefinition<object> CycleInSerialization = new(
            "LAMA0201",
            _category,
            "Cannot serialize the compile-time value of type '{0}' to a run-time value because it contains a cyclic reference or is nested more " +
            "than 32 levels deep.",
            Error,
            "A compile-time value contains a cyclic reference or is nested too deeply." );

        internal static readonly DiagnosticDefinition<object> MultidimensionalArray = new(
            "LAMA0202",
            _category,
            "Cannot serialize the compile-time array of type '{0}' to a run-time value because it has more than one dimension. Use a jagged " +
            "array instead.",
            Error,
            "Multidimensional arrays not supported." );

        internal static readonly DiagnosticDefinition<object> UnsupportedDictionaryComparer = new(
            "LAMA0203",
            _category,
            "Cannot serialize the compile-time dictionary to a run-time value because it has an unsupported equality comparer '{0}'. Only the " +
            "default comparer and, for string keys, the predefined StringComparer comparers such as StringComparer.OrdinalIgnoreCase are " +
            "supported.",
            Error,
            "Custom equality comparers not supported." );

        internal static readonly DiagnosticDefinition<(INamedTypeSymbol Type, INamedTypeSymbol BaseType)> MissingBaseConstructor = new(
            "LAMA0204",
            _category,
            "Cannot generate a compile-time serializer for '{0}' because the base type '{1}', declared in a referenced assembly, is " +
            "serializable but has neither an accessible parameterless constructor nor an accessible deserializing constructor with a single " +
            "parameter of type IArgumentsReader. Add one of these constructors to '{1}'.",
            Error,
            "Missing base parameterless or deserializing constructor." );

        internal static readonly DiagnosticDefinition<(INamedTypeSymbol Type, INamedTypeSymbol BaseType)> MissingBaseParameterlessConstructor = new(
            "LAMA0205",
            _category,
            "Cannot generate a compile-time serializer for '{0}' because the base type '{1}' is not compile-time serializable and does not have " +
            "a public or protected parameterless constructor. Make '{1}' implement ICompileTimeSerializable, or add a public or protected " +
            "parameterless constructor to it.",
            Error,
            "Missing base parameterless constructor." );

        internal static readonly DiagnosticDefinition<(INamedTypeSymbol Type, INamedTypeSymbol BaseTypeSerializer)> MissingBaseSerializerConstructor = new(
            "LAMA0207",
            _category,
            "Cannot generate a compile-time serializer for '{0}' because the serializer '{1}' of its base type does not have a public or " +
            "protected parameterless constructor. Add such a constructor to '{1}'.",
            Error,
            "Missing base serializer constructor." );

        internal static readonly DiagnosticDefinition<INamedTypeSymbol> AmbiguousManualSerializer = new(
            "LAMA0208",
            _category,
            "The compile-time serializable type '{0}' declares several nested types that implement ISerializer. Only one manual serializer is " +
            "allowed.",
            Error,
            "Ambiguous manual serializer." );

        internal static readonly DiagnosticDefinition<(INamedTypeSymbol Type, INamedTypeSymbol BaseType)> AmbiguousBaseSerializer = new(
            "LAMA0209",
            _category,
            "Cannot generate a compile-time serializer for '{0}' because the base type '{1}' declares several nested types that implement " +
            "ISerializer.",
            Error,
            "Ambiguous base serializer." );

        internal static readonly DiagnosticDefinition<INamedTypeSymbol> RecordSerializersNotSupported = new(
            "LAMA0210",
            _category,
            "Cannot generate a compile-time serializer for '{0}' because generated serializers are not currently supported for a positional record class or struct. "
            +
            "You can provide a manual serializer (public nested class) derived from ReferenceTypeSerializer (record classes) or ValueTypeSerializer (record structs) instead.",
            Error,
            "Generated serializers are not supported for positional records." );
    }
}
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using static Metalama.Framework.Diagnostics.Severity;

namespace Metalama.Framework.Engine.Templating.Immutability
{
#pragma warning disable SA1118 // Allow multi-line parameters.

    /// <summary>
    /// The diagnostics reported by <see cref="ImmutabilityValidator"/>.
    /// </summary>
    public static class ImmutabilityDiagnosticDescriptors
    {
        // Reserved range 880-899.

        private const string _category = "Metalama";

        private const string _because =
            "'{0}' must be written in an immutable style because it is an aspect, a fabric, a validator, or another type bound by the [ImmutableType] contract";

        private const string _staticBecause =
            "A static field of a compile-time type is shared by every aspect instance, every target and every compilation, "
            + "and at design time it keeps its value for as long as the analysis process runs.";

        /// <remarks>
        /// Reported only when the field can be written from outside the type that declares it. The assignments to a
        /// private field that is not read-only are reported instead by <see cref="MemberIsWrittenOutsideConstructor"/>.
        /// </remarks>
        internal static readonly DiagnosticDefinition<(string Type, string Member)> FieldIsNotReadOnly
            = new(
                "LAMA0880",
                Warning,
                _because + ", but the field '{1}' is neither read-only nor private, so not every assignment to it can be "
                         + "seen. Add the 'readonly' modifier, or make the field private.",
                "A field of a type that must be immutable can be written from outside the type.",
                _category );

        internal static readonly DiagnosticDefinition<(string Type, string Member)> PropertyHasSetter
            = new(
                "LAMA0881",
                Warning,
                _because + ", but the automatic property '{1}' has a setter that is not private, so not every assignment "
                         + "to it can be seen. Replace 'set' with 'init', make the setter private, or, for a positional record "
                         + "struct, declare it as a 'readonly record struct'.",
                "An automatic property of a type that must be immutable has a setter that is not private.",
                _category );

        internal static readonly DiagnosticDefinition<(string Type, string MemberType, string Path)> MemberIsNotImmutable
            = new(
                "LAMA0882",
                Warning,
                _because + ", but '{1}' is not immutable. Path: {2}.",
                "A member of a type that must be immutable is of a mutable type.",
                _category );

        internal static readonly DiagnosticDefinition<(string Type, string BaseType, string Path)> BaseTypeIsNotImmutable
            = new(
                "LAMA0883",
                Warning,
                _because + ", but its base type '{1}' is not immutable. Path: {2}.",
                "A type that must be immutable derives from a mutable type.",
                _category );

        internal static readonly DiagnosticDefinition<(string Type, string MemberType, string Path)> InterfaceIsNotImmutable
            = new(
                "LAMA0884",
                Warning,
                _because + ", but '{1}' is not marked [ImmutableType]. Marking it requires every implementation "
                         + "to be immutable, which Metalama verifies. Path: {2}.",
                "A member of a type that must be immutable is typed as an interface or abstract type that is not marked [ImmutableType].",
                _category );

        internal static readonly DiagnosticDefinition<(string Name, string ItemName)> UnknownDeclaredTypeName
            = new(
                "LAMA0885",
                Warning,
                "The name '{0}', declared in {1}, matches no type in this compilation. A generic type must be written "
                + "with its arity, as in System.Collections.Immutable.ImmutableArray`1.",
                "A declared immutable type name matches no type.",
                _category );

        internal static readonly DiagnosticDefinition<(string Type, string Source)> RedundantImmutableTypeAttribute
            = new(
                "LAMA0886",
                Warning,
                "The [ImmutableType] attribute on '{0}' has no effect, because '{0}' derives from or implements '{1}', "
                + "which already requires it. Remove the attribute.",
                "An [ImmutableType] attribute is redundant because the contract is already inherited.",
                _category );

        internal static readonly DiagnosticDefinition<(string Type, string Member)> MemberIsWrittenOutsideConstructor
            = new(
                "LAMA0887",
                Warning,
                _because + ", but '{1}' is assigned here, outside a constructor. Move the assignment into a constructor "
                         + "or an 'init' accessor.",
                "A member of a type that must be immutable is written outside a constructor.",
                _category );

        /// <remarks>
        /// A <c>ref</c> argument is not reported: it may only read, as <c>Volatile.Read( ref field )</c> does, and the
        /// signature does not say which.
        /// </remarks>
        internal static readonly DiagnosticDefinition<(string Type, string Member)> MemberIsPassedByReference
            = new(
                "LAMA0888",
                Warning,
                _because + ", but '{1}' is passed as an 'out' argument here, which writes it. Pass a local instead, and "
                         + "assign the member in a constructor.",
                "A member of a type that must be immutable is passed as an 'out' argument.",
                _category );

        internal static readonly DiagnosticDefinition<(string Type, string Member)> StaticFieldIsNotReadOnly
            = new(
                "LAMA0889",
                Warning,
                "'{0}' is a compile-time type, but the static member '{1}' is not read-only. " + _staticBecause
                + " Add the 'readonly' modifier to the field, or remove the setter of the property.",
                "A static field or automatic property of a compile-time type is not read-only.",
                _category );

        internal static readonly DiagnosticDefinition<(string Type, string Member, string MemberType, string Path)> StaticFieldIsNotImmutable
            = new(
                "LAMA0890",
                Warning,
                "'{0}' is a compile-time type, but the static member '{1}' is of type '{2}', which is not immutable. "
                + _staticBecause + " Path: {3}.",
                "A static field or automatic property of a compile-time type is of a mutable type.",
                _category );

        /// <remarks>
        /// One descriptor covers a member that is not read-only and a member of a mutable type, because the remedy is
        /// the same: the <c>[RunTime]</c> attribute removes the member from the compile-time assembly. The remedy is not
        /// offered for a compile-time type, where a run-time member is an error (LAMA0292).
        /// </remarks>
        internal static readonly DiagnosticDefinition<(string Type, string Member, string Cause)> StaticFieldMustBeRunTime
            = new(
                "LAMA0891",
                Warning,
                "'{0}' is a run-time-or-compile-time type, but the static member '{1}' {2}. " + _staticBecause
                + " Mark the member with [RunTime] so that it exists only at run time, or make it read-only and of an immutable type.",
                "A mutable static field or automatic property of a run-time-or-compile-time type is not marked [RunTime].",
                _category );

        /// <remarks>
        /// Reported only for a member that is read-only and of an immutable type, because LAMA0889, LAMA0890 or LAMA0891
        /// is reported otherwise. An immutable type can still retain a compilation: a symbol, a syntax tree or a
        /// delegate cannot change, but it keeps alive everything it references. Durability is evaluated by
        /// <c>StaticMemberDurabilityChecker</c> of <c>Metalama.Framework.Analyzers</c>.
        /// </remarks>
        internal static readonly DiagnosticDefinition<(string Type, string Member, string MemberType, string Path)> StaticFieldIsNotDurable
            = new(
                "LAMA0892",
                Warning,
                "'{0}' is loaded in the compiler process, but its static member '{1}' is of type '{2}', which is not durable. "
                + _staticBecause + " The member must therefore not reference a compilation, a syntax tree, a symbol or a "
                + "code model declaration. Path: {3}.",
                "A static field or automatic property of compile-time code is not durable.",
                _category );
    }
}

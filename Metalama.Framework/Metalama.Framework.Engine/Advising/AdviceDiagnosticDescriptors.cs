// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using static Metalama.Framework.Diagnostics.Severity;

#pragma warning disable SA1118

namespace Metalama.Framework.Engine.Advising
{
    public static class AdviceDiagnosticDescriptors
    {
        // Reserved range 500-599.

        private const string _category = "Metalama.Advices";

        // Sub-range 500-509: General introduction diagnostics.

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, IDeclaration TargetType, IDeclaration DeclaringType)>
            CannotIntroduceMemberAlreadyExists = new(
                "LAMA0500",
                "Cannot introduce a member into a type because it already exists.",
                "The aspect '{0}' cannot introduce member '{1}' into type '{2}' because it is already defined in type '{3}'. Use a different OverrideStrategy or skip the member.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, IDeclaration TargetType, IDeclaration DeclaringType)>
            CannotIntroduceOverrideOfSealed = new(
                "LAMA0502",
                "Cannot introduce a member into a type because it is sealed in a base class.",
                "The aspect '{0}' cannot introduce member '{1}' into type '{2}' because it is already defined in type '{3}' " +
                "and is static, non-virtual or sealed.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, IDeclaration TargetType, IDeclaration DeclaringType, IType
                ReturnType)>
            CannotIntroduceDifferentExistingReturnType = new(
                "LAMA0503",
                "Cannot introduce a member into a type because it has a different type or return type.",
                "The aspect '{0}' cannot introduce member '{1}' into type '{2}' because it is already defined in type '{3}' " +
                "and has a different type or return type '{4}'.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, IDeclaration TargetType, IDeclaration DeclaringType)>
            CannotIntroduceWithDifferentStaticity = new(
                "LAMA0504",
                "Cannot introduce a member into a type because the type already contains a member of the same name or signature but with a different staticity.",
                "The aspect '{0}' cannot introduce member '{1}' into type '{2}' because it is already defined in type '{3}' and " +
                "its IsStatic flag is opposite of the introduced member.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, IDeclaration TargetType)>
            CannotIntroduceInstanceMember = new(
                "LAMA0505",
                "Cannot introduce an instance member into a static type.",
                "The aspect '{0}' cannot introduce instance member '{1}' into a type '{2}' because it is static.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, IDeclaration TargetType, DeclarationKind DeclarationKind)>
            CannotIntroduceWithDifferentKind = new(
                "LAMA0506",
                "Cannot introduce a member into a type because another member of a different kind already exists.",
                "The aspect '{0}' cannot introduce member '{1}' into type '{2}' because there is already a {3} of the same name declared in the type or in a base type.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member)>
            CannotIntroduceStaticVirtualMember = new(
                "LAMA0507",
                "Cannot introduce a virtual member because it is also static.",
                "The aspect '{0}' cannot introduce virtual member '{1}' because it is also static.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member)>
            CannotIntroduceStaticSealedMember = new(
                "LAMA0508",
                "Cannot introduce a sealed member because it is also static.",
                "The aspect '{0}' cannot introduce sealed member '{1}' because it is also static.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, IDeclaration TargetType)>
            CannotIntroduceNewMemberWhenItAlreadyExists = new(
                "LAMA0509",
                "Cannot introduce a new member into a type because a member with the same name and signature already exists.",
                "The aspect '{0}' cannot introduce member '{1}' into type '{2}' with OverrideStrategy.New because the member is already declared in the type.",
                _category,
                Error );

        // Sub-range 510-519: Interface implementation diagnostics.

        internal static readonly DiagnosticDefinition<(string AspectType, INamedType TargetType, INamedType InterfaceType, IMember DeclarativeIntroduction,
                IMember InterfaceMember)>
            DeclarativeInterfaceMemberDoesNotMatch = new(
                "LAMA0511",
                "Declarative interface member does match the interface member return type.",
                "The aspect '{0}' cannot implicitly implement interface '{2}' in the type '{1}' because the aspect member '{3}'" +
                " marked with [InterfaceMember] attribute does not have the same return type as the corresponding interface member '{4}'.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, INamedType InterfaceType, INamedType TargetType)>
            InterfaceIsAlreadyImplemented = new(
                "LAMA0512",
                "Cannot implement an interface when the target type already implements it.",
                "The aspect '{0}' cannot implement interface '{1}' in the type '{2}' because the type already implements it and WhenExists is set to Fail.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, INamedType InterfaceType, INamedType TargetType)>
            CannotImplementCanonicalGenericInstanceOfGenericInterface = new(
                "LAMA0513",
                "Cannot implement a canonical generic instance of an interface.",
                "The aspect '{0}' cannot implement interface type '{1}' in the type '{2}' because it is a canonical generic instance. " +
                "Specify all type arguments of the generic interface type. If needed, use type parameters of the target type.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IMember InterfaceMember, INamedType TargetType, IMember ExistingDeclaration)>
            ImplicitInterfaceMemberAlreadyExists = new(
                "LAMA0514",
                "Cannot implement an implicit interface member when the target type already contains a declaration with the same signature.",
                "The aspect '{0}' cannot implement interface member '{1}' in the type '{2}' because the type already contains '{3}' "
                +
                "which has the same signature as the interface member and WhenExists of the interface member specification is set to Fail.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, INamedType InterfaceType, INamedType TargetType, OverrideStrategy Strategy)>
            InterfaceUnsupportedOverrideStrategy = new(
                "LAMA0516",
                "Using unsupported override strategy for interface type.",
                "The aspect '{0}' cannot implement interface '{1}' in type '{2}' with 'whenExists={3}' because it is not supported. " +
                "Only Ignore or Fail strategies are supported for interface types. You can use 'whenExists' on individual members.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IMember InterfaceProperty, INamedType TargetType, IMember TemplateMember, string
                AccessorKind)>
            InterfacePropertyIsMissingAccessor = new(
                "LAMA0517",
                "Cannot implement an interface property, the template is missing an accessor.",
                "The aspect '{0}' cannot implement an interface property '{1}' in the type '{2}' because the template '{3}' "
                +
                "is missing a '{4}' accessor.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IMember InterfaceProperty, INamedType TargetType, IMember TemplateMember, string
                AccessorKind)>
            ExplicitInterfacePropertyHasSuperficialAccessor = new(
                "LAMA0518",
                "Cannot implement an interface property, the template has superficial accessor.",
                "The aspect '{0}' cannot implement an interface property '{1}' in the type '{2}' explicitly because the template '{3}' "
                +
                "has an unexpected '{4}' accessor.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, INamedType InterfaceType, IMember TargetMember)>
            ImplicitInterfaceImplementationHasToBePublic = new(
                "LAMA0519",
                "Cannot implement an interface implicitly using non-public member.",
                "The aspect '{0}' cannot implicitly implement interface '{1}' using member '{2}', because it is not public.",
                _category,
                Error );

        // Sub-range 520-549: Various introduction diagnostics.
        internal static readonly DiagnosticDefinition<(string AspectType, IConstructor Constructor)>
            CannotIntroduceParameterIntoStaticConstructor = new(
                "LAMA0520",
                "Cannot introduce a parameter into a static constructor.",
                "The aspect '{0}' cannot introduce a parameter into '{1}' because it is a static constructor.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, INamedType AttributeType, IDeclaration TargetDeclaration)>
            AttributeAlreadyPresent = new(
                "LAMA0521",
                "Cannot introduce a custom attribute when the attribute is already present on the target declaration.",
                "The aspect '{0}' cannot introduce the custom attribute '{1}' into '{2}' because it this attribute is already present on the declaration and WhenExists is set to Fail.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration TargetType, OverrideStrategy OverrideStrategy)>
            CannotUseNewOverrideStrategyWithFinalizers = new(
                "LAMA0522",
                "Invalid override strategy when introducing a finalizer.",
                "The aspect '{0}' cannot introduce finalizer into type '{1}' because the specified override strategy '{2}' is not valid.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, INamedType Record)>
            CannotAddInitializerToRecord = new(
                "LAMA0524",
                "Cannot add an initializer to all constructors of a record.",
                "The aspect '{0}' cannot add an initializer to all constructors of the record '{1}', because the copy constructor cannot be changed. Consider adding initializers directly to the relevant constructors.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, IDeclaration TargetType)>
            CannotIntroduceVirtualToTargetType = new(
                "LAMA0525",
                "Cannot introduce a virtual member into a static type, sealed type or a struct.",
                "The aspect '{0}' cannot introduce virtual member '{1}' into a type '{2}' because it is static, sealed or a struct.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, IDeclaration TargetType)>
            CannotIntroduceIndexerWithoutParameters = new(
                "LAMA0526",
                "Cannot introduce an indexer without any parameter.",
                "The aspect '{0}' cannot introduce indexer '{1}' into type '{2}' because it has no parameter.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, IDeclaration TargetType)>
            CannotIntroduceStaticIndexer = new(
                "LAMA0527",
                "Cannot introduce a static indexer.",
                "The aspect '{0}' cannot introduce indexer '{1}' into type '{2}' because it is static.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member)>
            CannotOverrideNonPublicInterfaceMember = new(
                "LAMA0528",
                "Cannot override an interface member because it is not public.",
                "The aspect '{0}' cannot override the member '{1}' because it is not public.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member)>
            CannotOverrideNonVirtualInterfaceMember = new(
                "LAMA0529",
                "Cannot override an interface member because it is not virtual.",
                "The aspect '{0}' cannot override the member '{1}' because it is not virtual.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, string Parameter, IDeclaration TargetDeclaration, string ExistingParameterName)>
            CannotIntroduceParameterAlreadyExists = new(
                "LAMA0530",
                "Cannot introduce a parameter when a parameter with the same name already exists.",
                "The aspect '{0}' cannot introduce parameter '{1}' to '{2}' because the target declaration already has a parameter '{3}'.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, INamedType Type, INamespaceOrNamedType TargetNamespaceOrType)>
            CannotIntroduceNewTypeWhenItAlreadyExists = new(
                "LAMA0531",
                "Cannot introduce a new type because a type with the same name and type parameters already exists.",
                "The aspect '{0}' cannot introduce type '{1}' into '{2}' because the type already exists.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, IDeclaration TargetType)>
            CannotIntroduceAbstractMemberToNonAbstractType = new(
                "LAMA0532",
                "Cannot introduce an abstract member into a non-abstract type.",
                "The aspect '{0}' cannot introduce the abstract member '{1}' into type '{2}' because it is not abstract.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, OverrideStrategy OverrideStrategy)>
            CannotIntroduceAbstractMemberWithOverrideStrategy = new(
                "LAMA0533",
                "Cannot introduce an abstract member with override strategy Override.",
                "The aspect '{0}' cannot introduce the abstract member '{1}' with override strategy '{2}'.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, IDeclaration TargetType)>
            CannotIntroduceFieldIntoInterface = new(
                "LAMA0534",
                "Cannot introduce a field into an interface.",
                "The aspect '{0}' cannot introduce the field '{1}' into interface type '{2}'.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, IDeclaration TargetType)>
            CannotIntroducePartialMemberToNonPartialType = new(
                "LAMA0535",
                "Cannot introduce a partial member into a non-partial type.",
                "The aspect '{0}' cannot introduce the partial member '{1}' into type '{2}' because it is not partial.",
                _category,
                Error );
    }
}
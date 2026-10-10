// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
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
                "Cannot introduce a member into a type because the existing member of the base class cannot be overridden.",
                "The aspect '{0}' cannot introduce member '{1}' into type '{2}' because it is already defined in type '{3}' and is static, " +
                "non-virtual or sealed, so it cannot be overridden. Use OverrideStrategy.New to hide the existing member, or " +
                "OverrideStrategy.Ignore to keep it.",
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
                "The aspect '{0}' cannot introduce member '{1}' into type '{2}' because it is already defined in type '{3}', and one of the two " +
                "members is static while the other is not.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, IDeclaration TargetType)>
            CannotIntroduceInstanceMember = new(
                "LAMA0505",
                "Cannot introduce an instance member into a static type.",
                "The aspect '{0}' cannot introduce instance member '{1}' into type '{2}' because the type is static.",
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
                "An aspect member marked with [InterfaceMember] does not have the same type as the interface member.",
                "The aspect '{0}' cannot implement interface '{2}' in the type '{1}' because the aspect member '{3}', marked with the " +
                "[InterfaceMember] attribute, does not have the same type or return type as the corresponding interface member '{4}'.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, INamedType InterfaceType, INamedType TargetType)>
            InterfaceIsAlreadyImplemented = new(
                "LAMA0512",
                "Cannot implement an interface when the target type already implements it.",
                "The aspect '{0}' cannot implement interface '{1}' in the type '{2}' because the type already implements it. To skip or override " +
                "the existing implementation, set the whenExists parameter of ImplementInterface to OverrideStrategy.Ignore or " +
                "OverrideStrategy.Override.",
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
                "The aspect '{0}' cannot implement the interface member '{1}' in the type '{2}' because the type already contains '{3}', which has " +
                "the same signature. Set the WhenExists property of the [InterfaceMember] attribute to Ignore or MakeExplicit, or pass " +
                "OverrideStrategy.Override to ImplementInterface.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, INamedType InterfaceType, INamedType TargetType, OverrideStrategy Strategy)>
            InterfaceUnsupportedOverrideStrategy = new(
                "LAMA0516",
                "The override strategy is not supported when implementing an interface.",
                "The aspect '{0}' cannot implement the interface '{1}' in the type '{2}' because the strategy 'whenExists={3}' is not supported for " +
                "interfaces. Use Fail, Ignore, or Override. To handle conflicts with individual members, use the WhenExists property of the " +
                "[InterfaceMember] attribute.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IMember InterfaceProperty, INamedType TargetType, IMember TemplateMember, string
                AccessorKind)>
            InterfacePropertyIsMissingAccessor = new(
                "LAMA0517",
                "Cannot implement an interface property because the template is missing an accessor.",
                "The aspect '{0}' cannot implement the interface property '{1}' in the type '{2}' because the template '{3}' does not have the " +
                "'{4}' accessor required by the interface property.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IMember InterfaceProperty, INamedType TargetType, IMember TemplateMember, string
                AccessorKind)>
            ExplicitInterfacePropertyHasSuperficialAccessor = new(
                "LAMA0518",
                "Cannot implement an interface property explicitly because the template has an accessor that the interface property does not have.",
                "The aspect '{0}' cannot implement the interface property '{1}' in the type '{2}' explicitly because the template '{3}' has the " +
                "'{4}' accessor, which the interface property does not have. Remove the accessor from the template or implement the property " +
                "implicitly.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, INamedType InterfaceType, IMember TargetMember)>
            ImplicitInterfaceImplementationHasToBePublic = new(
                "LAMA0519",
                "Cannot implement an interface implicitly with a non-public member.",
                "The aspect '{0}' cannot implicitly implement the interface '{1}' with the member '{2}' because this member or one of its accessors " +
                "is not public. Make the member and its accessors public, or set the IsExplicit property of the [InterfaceMember] attribute to " +
                "true.",
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
                "The aspect '{0}' cannot introduce the custom attribute '{1}' into '{2}' because this attribute is already present on the " +
                "declaration. To keep or replace the existing attribute, set the 'whenExists' parameter to 'Ignore' or 'Override'.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration TargetType, OverrideStrategy OverrideStrategy)>
            CannotUseNewOverrideStrategyWithFinalizers = new(
                "LAMA0522",
                "Invalid override strategy when introducing a finalizer.",
                "The aspect '{0}' cannot introduce a finalizer into the type '{1}' because the override strategy '{2}' is not supported for " +
                "finalizers. Use 'Fail', 'Ignore', or 'Override'.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, INamedType Record)>
            CannotAddInitializerToRecord = new(
                "LAMA0524",
                "Cannot add an initializer to all constructors of a record.",
                "The aspect '{0}' cannot add an initializer to all constructors of the record '{1}', because the copy constructor cannot be changed. Consider adding initializers directly to the relevant constructors.",
                _category,
                Error );

        // LAMA0525 was removed. It was previously used to report errors when introducing virtual members into
        // sealed types or structs. Virtual introductions into sealed types and structs are now silently accepted
        // (the virtual modifier is dropped).

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, IDeclaration TargetType)>
            CannotIntroduceIndexerWithoutParameters = new(
                "LAMA0526",
                "Cannot introduce an indexer without any parameter.",
                "The aspect '{0}' cannot introduce the indexer '{1}' into the type '{2}' because the indexer has no parameter. An indexer must have " +
                "at least one parameter.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, IDeclaration TargetType)>
            CannotIntroduceStaticIndexer = new(
                "LAMA0527",
                "Cannot introduce a static indexer.",
                "The aspect '{0}' cannot introduce the indexer '{1}' into the type '{2}' because the indexer is static. C# does not support static " +
                "indexers.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member)>
            CannotOverrideNonPublicInterfaceMember = new(
                "LAMA0528",
                "Cannot implement an interface member by overriding an existing member that is not public.",
                "The aspect '{0}' cannot override the member '{1}' to implement an interface member because '{1}' is not public. Make '{1}' public, " +
                "or implement the interface member explicitly.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member)>
            CannotOverrideNonVirtualInterfaceMember = new(
                "LAMA0529",
                "Cannot implement an interface member by overriding an inherited member that is not virtual or is sealed.",
                "The aspect '{0}' cannot override the member '{1}' to implement an interface member because '{1}' is inherited from a base type and " +
                "is either not virtual or sealed. Make '{1}' virtual and not sealed.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, string Parameter, IDeclaration TargetDeclaration, string ExistingParameterName)>
            CannotIntroduceParameterAlreadyExists = new(
                "LAMA0530",
                "Cannot introduce a constructor parameter when an aspect has already introduced a parameter with the same name.",
                "The aspect '{0}' cannot introduce the parameter '{1}' into '{2}' because an aspect has already introduced a parameter named '{3}' " +
                "into this constructor.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, INamedType Type, INamespaceOrNamedType TargetNamespaceOrType)>
            CannotIntroduceNewTypeWhenItAlreadyExists = new(
                "LAMA0531",
                "Cannot introduce a type because a type with the same name and the same number of type parameters already exists.",
                "The aspect '{0}' cannot introduce the type '{1}' into '{2}' because '{2}' already contains or inherits a type with the same name " +
                "and the same number of type parameters. Use a different name, or specify another OverrideStrategy.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, IDeclaration TargetType)>
            CannotIntroduceAbstractMemberToNonAbstractType = new(
                "LAMA0532",
                "Cannot introduce an abstract member into a non-abstract type.",
                "The aspect '{0}' cannot introduce the abstract member '{1}' into the type '{2}' because '{2}' is not abstract.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IDeclaration Member, OverrideStrategy OverrideStrategy)>
            CannotIntroduceAbstractMemberWithOverrideStrategy = new(
                "LAMA0533",
                "Cannot introduce an abstract member with override strategy Override or New.",
                "The aspect '{0}' cannot introduce the abstract member '{1}' with the override strategy '{2}' because an introduced abstract member " +
                "cannot override or hide an existing member. Use the override strategy 'Fail' or 'Ignore'.",
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
                "The aspect '{0}' cannot introduce the partial member '{1}' into the type '{2}' because the type is not partial. Add the partial " +
                "modifier to the type.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IParameter IntroducedParameter, IConstructor Forwarder, string ReturnedKind)>
            InvalidPullActionForForwardingConstructor = new(
                "LAMA0536",
                "Invalid pull action for a forwarding constructor.",
                "The aspect '{0}' cannot generate the forwarding constructor '{2}' because the pull strategy returned '{3}' for the parameter " +
                "'{1}'. For a forwarding constructor, the pull strategy must provide a value, for instance with PullAction.UseExpression, and must " +
                "not return PullAction.None.",
                _category,
                Error );

        // Sub-range 540-549: Extension block introduction diagnostics.

        internal static readonly DiagnosticDefinition<(string AspectType, IType ReceiverType, INamedType TargetType)>
            ExtensionBlockTargetMustBeStaticClass = new(
                "LAMA0540",
                "Cannot introduce an extension block into a non-static class.",
                "The aspect '{0}' cannot introduce an extension block for type '{1}' into '{2}' because the target type is not a static class.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, IType ReceiverType, INamedType TargetType)>
            CannotIntroduceExtensionBlockIntoExtensionBlock = new(
                "LAMA0541",
                "Cannot introduce an extension block into another extension block.",
                "The aspect '{0}' cannot introduce an extension block for type '{1}' into '{2}' because the target is an extension block. Extension blocks can only be introduced into static classes.",
                _category,
                Error );

        // Sub-range 550-559: Initialization diagnostics.

        internal static readonly DiagnosticDefinition<(string AspectType, INamedType TargetType)>
            InitializeNotVirtual = new(
                "LAMA0550",
                "The Initialize method of a non-sealed class must be public and virtual.",
                "The aspect '{0}' cannot add an initializer to the 'Initialize' method of type '{1}' because the method is not both 'public' and " +
                "'virtual' (or 'override'). Since '{1}' is not sealed, its 'Initialize' method must be 'public virtual' so that derived types can " +
                "extend the initialization. Make the method 'public virtual', or make the type 'sealed'.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectType, INamedType TargetType, INamedType BaseType)>
            OnConstructedBaseWithoutContextConstructor = new(
                "LAMA0551",
                "Base type has OnConstructed method but no constructor accepting InitializationContext.",
                "The aspect '{0}' targets type '{1}' whose base type '{2}' defines an 'OnConstructed(InitializationContext)' method but has no instance constructor accepting an 'InitializationContext' parameter. The base type must provide such a constructor (and call 'OnConstructed' from it, guarded by 'IsHandled(InitializationSlot.OnConstructed)') so that derived types can pass 'context.Descend(InitializationSlot.OnConstructed)' and skip the base's OnConstructed call.",
                _category,
                Error );
    }
}
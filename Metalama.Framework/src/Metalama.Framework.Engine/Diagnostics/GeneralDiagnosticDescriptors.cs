// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Utilities.UserCode;
using Microsoft.CodeAnalysis;
using System;
using static Metalama.Framework.Diagnostics.Severity;

#pragma warning disable SA1118

namespace Metalama.Framework.Engine.Diagnostics
{
    public sealed class GeneralDiagnosticDescriptors
    {
        // Reserved range 0-99

        private const string _category = "Metalama.General";

        internal static readonly DiagnosticDefinition<(string Message, string File)> UnhandledException =
            new(
                "LAMA0001",
                _category,
                "Unexpected exception occurred in Metalama: {0} Exception details are in '{1}'. " +
                "Please report this issue at https://www.postsharp.net/support and attach this file to the ticket. You may want to remove sensitive data from the report.",
                Error,
                "Unexpected exception in Metalama." );

        internal static readonly
            DiagnosticDefinition<(string AspectType, DeclarationKind DeclarationKind, IDeclaration Declaration, ITypeSymbol InterfaceType)>
            AspectAppliedToIncorrectDeclaration =
                new(
                    "LAMA0003",
                    _category,
                    "Aspect '{0}' cannot be applied to {1} '{2}', because this aspect does not implement the '{3}' interface.",
                    Error,
                    "Aspect applied to incorrect kind of declaration." );

        internal static readonly DiagnosticDefinition<(IDeclaration Member, int RequiredArgumentsCount, int ActualArgumentsCount)> MemberRequiresNArguments =
            new( "LAMA0012", _category, "Member '{0}' requires {1} arguments but received {2}.", Error, "Member requires number of arguments." );

        internal static readonly DiagnosticDefinition<(IDeclaration Member, int RequiredArgumentsCount, int ActualArgumentsCount)>
            MemberRequiresAtLeastNArguments =
                new( "LAMA0013", _category, "Member '{0}' requires at least {1} arguments but received {2}.", Error, "Member requires more arguments." );

        internal static readonly DiagnosticDefinition<IMemberOrNamedType> CannotProvideInstanceForLocalFunction =
            new(
                "LAMA0018",
                _category,
                "{0} is a local function, so it Cannot be invoked with a non-null instance.",
                Error,
                "Cannot provide instance for a local function." );

        internal static readonly DiagnosticDefinition<(string Expression, string ParameterName, IMemberOrNamedType Method)>
            CannotPassExpressionToByRefParameter =
                new(
                    "LAMA0019",
                    _category,
                    "Cannot pass the expression '{0}' to the '{1}' parameter of method '{2}' because the parameter is 'out' or 'ref'.",
                    Error,
                    "Cannot use an expression in an out or ref parameter." );

        internal static readonly DiagnosticDefinition<(string TypeName, string? AssemblyName)> CannotFindType =
            new( "LAMA0020", _category, "Cannot find the type '{0}' of assembly '{1}'.", Error, "Cannot find a type" );

        internal static readonly DiagnosticDefinition<string> CycleInAspectOrdering =
            new(
                "LAMA0021",
                _category,
                "A cycle was found in the specifications of aspect ordering between the following aspect part: {0}.",
                Error,
                "A cycle was found in aspect ordering." );

        internal static readonly DiagnosticDefinition<(string ParentType, string ChildType)> CannotAddChildAspectToPreviousPipelineStep = new(
            "LAMA0022",
            _category,
            "The aspect '{0}' cannot add a child aspect to of type '{1}' because the '{1}' aspect is processed before '{0}'.",
            Error,
            "Cannot add an aspect to a previous step of the compilation pipeline." );

        internal static readonly DiagnosticDefinition<(string AspectType, string MethodName)> AspectMustHaveExactlyOneTemplateMember = new(
            "LAMA0025",
            "The aspect type must have exactly one member of a given name otherwise it cannot be used as a dynamic advice.",
            "The type '{0}' must have exactly one member named '{1}'.",
            _category,
            Error );

        internal static readonly DiagnosticDefinition<AssemblyIdentity> CannotFindCompileTimeAssembly = new(
            "LAMA0027",
            _category,
            "The assembly '{0}' required at compile-time cannot be found.",
            Error,
            "Cannot find an assembly required by the compile-time assembly." );

        internal static readonly DiagnosticDefinition<(string TemplateName, string ClassName)>
            TemplateWithSameNameAlreadyDefined = new(
                "LAMA0032",
                _category,
                "The class '{1}' defines several templates named '{0}'. Template names must be unique.",
                Error,
                "The class already defines a template of the same name." );

        internal static readonly DiagnosticDefinition<(string ClassName, string MemberName)>
            MemberDoesNotHaveTemplateAttribute = new(
                "LAMA0033",
                _category,
                "The class '{0}' defines a member named '{1}', but the member is not annotated with the [Template] custom attribute.",
                Error,
                "The member does not have a template custom attribute." );

        internal static readonly DiagnosticDefinition<(string Layer1, string Layer2)> UnorderedLayers = new(
            "LAMA0035",
            _category,
            "The aspect layers '{0}' and '{1}' are not strongly ordered. Add an [assembly: " + nameof(AspectOrderAttribute) +
            $"(...)] attribute to specify the order relationship between these two layers or disable the {MSBuildPropertyNames.MetalamaRequireOrderedAspects} build option.",
            Error,
            "Two layers are not strongly ordered." );

        internal static readonly DiagnosticDefinition<(string TemplateName, string ClassName, string BaseClassName)>
            TemplateWithSameNameAlreadyDefinedInBaseClass = new(
                "LAMA0036",
                _category,
                "The class '{1}' defines a new template named '{0}', but the base class '{2}' already defines a template of the same name. Template names must be unique.",
                Error,
                "The class already defines a template of the same name." );

        internal static readonly DiagnosticDefinition<(string AspectName, DeclarationKind DeclarationKind, IDeclaration Target, FormattableString Reason)>
            AspectNotEligibleOnTarget = new(
                "LAMA0037",
                _category,
                "The aspect '{0}' cannot be applied to the {1} '{2}' because {3}.",
                Error,
                "The aspect cannot be applied to a declaration because eligibility conditions are not met." );

        internal static readonly DiagnosticDefinition<(FormattableString Predecessor, IDeclaration Child, IDeclaration Parent)>
            CanAddChildAspectOnlyUnderParent = new(
                "LAMA0038",
                Error,
                "The {0} cannot add a child aspect to '{1}' because it is not contained in '{2}'.",
                "A parent aspect or fabric can add child aspects only under its target declaration.",
                _category );

        internal static readonly DiagnosticDefinition<(FormattableString Predecessor, string AspectType, IDeclaration Child, FormattableString Reason)>
            IneligibleChildAspect = new(
                "LAMA0039",
                Error,
                "The {0} cannot add a child aspect of type '{1}' to '{2}' because {3}.",
                "A parent aspect or fabric can add child aspects only under its target declaration.",
                _category );

        internal static readonly DiagnosticDefinition<Type>
            TypeMustHavePublicDefaultConstructor = new(
                "LAMA0040",
                _category,
                "The  type '{0}' must have a default constructor.",
                Error,
                "The type must have a default constructor." );

        internal static readonly DiagnosticDefinition<(UserCodeDescription TemplateSymbol, string ExceptionType, string ExceptionMessage, string Details)>
            ExceptionInUserCode
                = new(
                    "LAMA0041",
                    "Exception in user code",
                    "'Exception of type '{1}' thrown while {0}: {2}. Exception details are in '{3}'. To attach a debugger to the compiler, use the " +
                    " '-p:MetalamaDebugCompiler=True' command-line option.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(FormattableString Predecessor, IDeclaration Child, IDeclaration Parent)>
            CanSetOptionsOnlyUnderParent = new(
                "LAMA0044",
                Error,
                "The {0} cannot set options for '{1}' because it is not contained in '{2}'.",
                "An aspect or fabric can set options only under its target declaration.",
                _category );

        internal static readonly DiagnosticDefinition<INamedTypeSymbol>
            LiveTemplateMustHaveDefaultConstructor = new(
                "LAMA0045",
                _category,
                $"The class '{{0}}' must have a default constructor because of the [{nameof(EditorExperienceAttribute)}({nameof(EditorExperienceAttribute.SuggestAsLiveTemplate)} = true)] attribute.",
                Error,
                "Live templates must have a default constructor." );

        public static readonly DiagnosticDefinition<INamedType>
            TypeNotPartial
                = new(
                    "LAMA0048",
                    "The type must be made partial.",
                    "Aspects add members to '{0}' but it is not marked as 'partial'. Make the type 'partial' to make it possible to "
                    + "reference aspect-generated artefacts from source code.",
                    _category,
                    Warning );

        internal static readonly DiagnosticDefinition<(string Message, string File)> IgnorableUnhandledException =
            new(
                "LAMA0049",
                _category,
                "Unexpected exception occurred in Metalama: {0} Exception details are in '{1}'. " +
                " Please report this issue at https://www.postsharp.net/support and attach this file to the ticket.",
                Warning,
                "Unexpected exception in Metalama." );

        internal static readonly DiagnosticDefinition<(string WeaverType, string AspectType)> CannotFindAspectWeaver =
            new(
                "LAMA0050",
                _category,
                "The weaver type '{0}' required to weave aspect '{1}' is not found in the project.",
                Error,
                "Cannot find an aspect weaver." );

        internal static readonly DiagnosticDefinition<string[]> PreviewCSharpVersionNotSupported =
            new(
                "LAMA0051",
                Error,
                "Metalama does not support the 'preview' language version. Change the LangVersion property of your csproj file to one of the following supported values: {0}. "
                + "If you want to use preview features at your own risks, set the MSBuild property 'MetalamaAllowPreviewLanguageFeatures' to 'true'. It may work if you don't use preview features in templates.",
                "Metalama does not support the 'preview' C# language version",
                _category );

        internal static readonly DiagnosticDefinition<(string SelectedVersion, string PropertyName, string[] SupportedVersions)> CSharpVersionNotSupported =
            new(
                "LAMA0052",
                Error,
                "The C# language version '{0}' is not supported. Change the <{1}> property of your project file to one of the following supported values: {2}."
                + " Do not use 'latest' or `latestMajor` because it will be inconsistently interpreted if you use a more recent .NET SDK or IDE than Metalama.",
                "The selected C# language version is not supported",
                _category );

        internal static readonly DiagnosticDefinition MissingMetalamaPreprocessorSymbol =
            new(
                "LAMA0053",
                Error,
                "Metalama is enabled in this project, but the METALAMA preprocessor symbol is not defined.",
                "Metalama is enabled in this project, but the METALAMA preprocessor symbol is not defined.",
                _category );

        internal static readonly DiagnosticDefinition<(string[] SelectedVersions, string SupportedVersion)> MetalamaVersionNotSupported =
            new(
                "LAMA0054",
                Error,
                "The project references the version(s) {0} of Metalama.Framework, but the current compiler version requires the version '{1}' or lower.",
                "The project has referenced to unsupported versions of Metalama",
                _category );

        internal static readonly
            DiagnosticDefinition<(string AspectType, IDeclaration ParentTarget, DeclarationKind ParentTargetKind, IDeclaration ChildTarget, DeclarationKind
                ChildTargetKind)> CannotAddAspectToPreviousPipelineStep = new(
                "LAMA0055",
                _category,
                "The aspect '{0}' applied to {2} '{1}' cannot add an aspect of the same type to {4} '{3}' because the {4} is not contained the {2}.",
                Error,
                "Cannot add an aspect to a previous step of the compilation pipeline." );

        internal static readonly DiagnosticDefinition MetalamaNotInstalled =
            new(
                "LAMA0057",
                Error,
                "Metalama is not enabled in this project.",
                "Metalama is not enabled in this project.",
                _category );

        internal static readonly DiagnosticDefinition<ISymbol> GenericAspectTypeNotSupported =
            new(
                "LAMA0059",
                Error,
                "The type '{0}' is not a valid aspect type because it is generic. Generic aspect types are not yet supported.",
                "Non-abstract generic aspect types are not supported.",
                _category );

        internal static readonly DiagnosticDefinition<ISymbol> RefMembersNotSupported =
            new(
                "LAMA0060",
                Error,
                "'{0}' cannot be used as a template because it returns 'ref'. This feature is not yet supported.",
                "'ref' members cannot be used as templates.",
                _category );

        internal static readonly DiagnosticDefinition<(AssemblyIdentity AssemblyIdentity, string Version)>
            DependencyMustBeRecompiled =
                new(
                    "LAMA0061",
                    Error,
                    "The referenced assembly '{0}' has been compiled with Metalama {1}. It must be recompiled with the current version because " +
                    "backward compatibility of compiled assemblies has been broken.",
                    "The referenced assembly must be recompiled with a more recent version of Metalama.",
                    _category );

        internal static readonly DiagnosticDefinition<(IMember Member, INamedType TargetType, InvokerOptions InvokerOptions)>
            CantInvokeBaseOrCurrentOutsideTargetType =
                new(
                    "LAMA0063",
                    Error,
                    "Cannot invoke member '{0}' when specifying InvokerOptions.{2} here, because it does not belong to the template target type '{1}'.",
                    "Cannot invoke a member that does not belong to the template target type when specifying InvokerOptions.Base or InvokerOptions.Current.",
                    _category );

        internal static readonly DiagnosticDefinition<(IMember Member, string ExceptionMessage)>
            CantGetMemberInitializer =
                new(
                    "LAMA0064",
                    Error,
                    "Couldn't get initializer for '{0}': {1}",
                    "Couldn't get initializer for member.",
                    _category );

        internal static readonly DiagnosticDefinition<(string OptionName, DeclarationKind DeclarationKind, IDeclaration Target, FormattableString Reason)>
            OptionNotEligibleOnTarget = new(
                "LAMA0065",
                _category,
                "The option '{0}' cannot be applied to the {1} '{2}' because {3}.",
                Error,
                "The option cannot be applied to a declaration because eligibility conditions are not met." );

        internal static readonly DiagnosticDefinition
            CodeFormattingEnabled = new(
                "LAMA0066",
                _category,
                "Formatting of generated code is enabled. Build performance could be significantly affected.",
                Warning,
                "Code formatting is enabled. Build performance could be significantly affected." );

        internal static readonly DiagnosticDefinition<IConstructor>
            CannotInvokeStaticConstructor =
                new( "LAMA0067", _category, "Constructor '{0}' cannot be invoked because it is static.", Error, "Static constructor cannot be invoked." );

        internal static readonly DiagnosticDefinition
            ErrorsInSourceCode =
                new( "LAMA0068", _category, "Compile-time code contains C# errors.", Error, "Compile-time code contains C# errors." );

        internal static readonly DiagnosticDefinition<(FormattableString Predecessor, IDeclaration Child, IDeclaration Parent)>
            CanReportDiagnosticOnlyUnderParent = new(
                "LAMA0069",
                Error,
                "The {0} cannot add a report or suppress a diagnostic to or from '{1}' because it is not contained in '{2}'.",
                "An aspect or fabric can report or suppress a diagnostic  only under its target declaration.",
                _category );

        // TODO: Use formattable string (C# does not seem to find extension methods).
        internal static readonly DiagnosticDefinition<string>
            UnsupportedFeature = new(
                "LAMA0099",
                "Feature is not yet supported.",
                "Feature is not yet supported: {0}",
                _category,
                Error );
    }
}
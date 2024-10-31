// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CompileTime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using static Metalama.Framework.Diagnostics.Severity;

namespace Metalama.Framework.Engine.Templating
{
#pragma warning disable SA1118 // Allow multi-line parameters.

    public static class TemplatingDiagnosticDescriptors
    {
        // Reserved ranges 100-119, 220-299

        private const string _category = "Metalama.Template";

        internal static readonly DiagnosticDefinition<string> LanguageFeatureIsNotSupported
            = new(
                "LAMA0101",
                "The C# language feature is not supported.",
                "'{0}' is not supported in a template.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string Expression, string ActualScope, string ExpectedScope, string Context)> ScopeMismatch
            = new(
                "LAMA0104",
                "The expression is expected to be of a different scope (run-time or compile-time)",
                "The expression '{0}' is {1} but it is expected to be {2} because the expression appears in {3}.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> SplitVariables
            = new(
                "LAMA0105",
                "Build-time and run-time local variables cannot be mixed in the same declaration. Split them into different declarations; "
                + "one for run-time variables, and one for compile-time variables",
                "Local variables {0} cannot be declared in the same declaration. "
                + "Split them into different declarations; one for run-time variables, and one for compile-time variables",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string VariableName, string RunTimeCondition)> CannotSetCompileTimeVariableInRunTimeConditionalBlock
            = new(
                "LAMA0108",
                "Cannot set an outside compile-time variable in a block whose execution depends on a run-time condition",
                "Cannot set the compile-time variable '{0}' here because it is part of a block whose execution depends on the run-time condition '{1}' and it was not declared inside the block. "
                +
                "Move the assignment out of the run-time-conditional block or move the variable into the block.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> UndeclaredRunTimeIdentifier
            = new(
                "LAMA0109",
                "The run-time identifier was not declared",
                "The run-time identifier '{0}' was not defined.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string LoopKind, string RunTimeCondition)> CannotHaveCompileTimeLoopInRunTimeConditionalBlock
            = new(
                "LAMA0110",
                "Cannot have a compile-time loop in a block whose execution depends on a run-time condition",
                "The compile-time {0} loop is not allowed here because it is a part of block whose execution depends on the run-time condition '{1}'. " +
                "Move the loop out of the run-time-conditional block.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectName, string AssemblyName)>
            CannotFindAspectInCompilation
                = new(
                    "LAMA0113",
                    "An aspect type defined in a reference assembly could not be found in the compilation",
                    "Cannot find in the current compilation the aspect type '{0}' defined in the aspect library '{1}'.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(IDeclaration Advice, string Expression, IDeclaration TargetDeclaration, DeclarationKind TargetKind,
                FormattableString Explanation)>
            CannotUseThisInStaticContext
                = new(
                    "LAMA0114",
                    "Cannot use 'meta.This' from a static context",
                    "The advice '{0}' cannot use '{1}' in an advice applied to {3} '{2}' because {4}.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(IDeclaration Advice, string Expression, IDeclaration TargetDeclaration, DeclarationKind TargetKind,
                string MissingKind, string? AlternativeSuggestion)>
            MetaMemberNotAvailable
                = new(
                    "LAMA0115",
                    "Cannot use a meta member in the current context",
                    "The advice '{0}' cannot use '{1}' in an advice applied to {3} '{2}' because there is no '{4}'.{5}",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(ISymbol DeclaringSymbol, ISymbol ReferencedSymbol, TemplatingScope DeclaringScope)>
            CannotReferenceCompileTimeOnly
                = new(
                    "LAMA0117",
                    "Cannot reference a compile-time-only declaration in a non-compile-time-only declaration.",
                    "Cannot reference '{1}' in '{0}' because '{1}' is compile-time-only but '{0}' is {2}. " +
                    "Consider adding [CompileTime] to '{0}', or do not use '{1}' in '{0}'.'",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<ISymbol>
            CompileTimeTypeNeedsRebuild
                = new(
                    "LAMA0118",
                    "The compile-time type needs rebuild.",
                    "The compile-time type '{0}' has been modified since the last build. Metalama will stop analyzing this solution until the "
                    + "next build and you may get errors related to the absence of generated source. "
                    + "To resume analysis, finish the work on all compile-time logic, then build the project (even if the run-time code still has issues).",
                    _category,
                    Warning );

        internal static readonly DiagnosticDefinition<(ISymbol Declaration, string Namespace, string AttributeName)>
            CompileTimeCodeNeedsNamespaceImport
                = new(
                    "LAMA0119",
                    "The declaration contains compile-time code but it does not import the proper namespaces.",
                    "The compile-time declaration '{0}' contains compile-time code but it does not explicitly import any of the the '{1}' namespaces. "
                    + "This may cause an inconsistent design-time experience. Add the [{2}] attribute to '{0}' and import this namespace explicitly.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(ISymbol ReferencedDeclaration, ISymbol ReferencingDeclaration)>
            OnlyMethodsCanBeSubtemplates
                = new(
                    "LAMA0220",
                    "A template can only reference other templates that are methods.",
                    "The declaration '{0}' cannot be referenced from '{1}', because it is a template, but not a method.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<string>
            CannotUseThisInRunTimeContext
                = new(
                    "LAMA0221",
                    "Cannot use 'this' when a run-time expression is expected.",
                    "Cannot use 'this' in expression '{0}' because a run-time expression is expected, and 'this' "
                    + "in a template is a compile-time keyword. Use 'meta.This' instead.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<string>
            CannotEmitCompileTimeAssembly
                = new(
                    "LAMA0222",
                    "Error compiling the compile-time assembly.",
                    "The compile-time project could not be compiled. In most cases, this is due to a problem in your code and can be diagnosed " +
                    "using the other reported errors. If, however, you believe this is due to a bug in Metalama, please report the issue and include diagnostic "
                    +
                    "information available in '{0}'.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(string MethodName, IDeclaration TargetDeclaration)>
            CannotUseSpecificProceedInThisContext
                = new(
                    "LAMA0223",
                    "Cannot use a specific Proceed variant in the current context.",
                    "Cannot use the {0} method in '{1}' because the return type of the method is not compatible with the {0} method.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<string>
            CannotUseDynamicInUninitializedLocal
                = new(
                    "LAMA0224",
                    "Cannot declare local variable with the dynamic type if the variable is not initialized.",
                    "The 'dynamic' keyword cannot be used in the local variable '{0}' because it is not initialized.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(ISymbol Symbol, ISymbol RunTimeSymbol, ISymbol CompileTimeSymbol)> TemplatingScopeConflict
            = new(
                "LAMA0226",
                "The syntax is invalid because it combines run-time and compile-time elements.",
                "'{0}' is invalid because '{1}' is run-time but '{2}' is compile-time.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> InvalidDynamicTypeConstruction
            = new(
                "LAMA0227",
                "'dynamic' is forbidden as a generic parameter type or array element type in a template.",
                "The type '{0}' is forbidden in a template: 'dynamic' cannot be used as a generic argument type, an array element type, a tuple element type or a ref type.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<ITypeSymbol> NeutralTypesForbiddenInNestedRunTimeTypes
            = new(
                "LAMA0229",
                "Types that are both compile-time and run-time are forbidden in run-time-only types.",
                "The type '{0}' cannot be [CompileOrRunTime] because it is nested in a run-time-only type.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<ITypeSymbol> NestedCompileTypesMustBePrivate
            = new(
                "LAMA0230",
                "Nested compile-time types must have private accessibility.",
                "The compile-time type '{0}' must have private visibility because it is nested in a run-time-type.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(ITypeSymbol NestedType, Type TypeFabric)> RunTimeTypesCannotHaveCompileTimeTypesExceptTypeFabrics
            = new(
                "LAMA0231",
                "Compile-time types cannot be nested in run-time types, except for type fabrics.",
                "The compile-time type '{0}' cannot be nested in a run-time type. The only compile-time type that can be nested in run-time type is a class inheriting '{1}'.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> TemplateUsesUnsupportedLanguageVersion
            = new(
                "LAMA0232",
                "Template code must be written in the specified C# version.",
                "Template code must be written in C# {0}.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(ISymbol DeclaringSymbol, ISymbol ReferencedSymbol, string? Explanation)>
            CannotUseTemplateOnlyOutOfTemplate
                = new(
                    "LAMA0233",
                    "Cannot use a template-only method out of a template.",
                    "Cannot use '{1}' in '{0}' because it is only allowed inside a template.{2}",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<ISymbol> PartiallyUnresolvedSymbolInTemplate
            = new(
                "LAMA0235",
                "The definition of a type or member used in a template is partially invalid.",
                "The definition of the type or member '{0}' is invalid. Metalama could report irrelevant errors in the current template.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(ISymbol DeclaringSymbol, ISymbol ReferencedSymbol, TemplatingScope DeclaringSymbolScope)>
            CannotReferenceRunTimeOnly
                = new(
                    "LAMA0236",
                    "Cannot reference a run-time-only declaration in a compile-time-only declaration.",
                    "Cannot reference '{1}' in '{0}' (except for templates) because '{1}' is run-time-only but '{0}' is {2}.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<ISymbol>
            AbstractTemplateCannotHaveRunTimeSignature
                = new(
                    "LAMA0237",
                    "Abstract templates cannot include a run-time-only type in their signature.",
                    "The template '{0}' cannot be abstract because it has a run-time-ony signature.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(ISymbol Member, INamedTypeSymbol DeclaringType, TemplatingScope DeclaringTypeScope)>
            OnlyNamedTemplatesCanHaveDynamicSignature
                = new(
                    "LAMA0238",
                    "Only templates of [Template] kind can have a dynamic type or signature.",
                    "'{0}' cannot be of 'dynamic' type because the type '{1}' is {2} and '{0}' is not a template.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(string ParentExpression, string Expression1, string Scope1, string Expression2, string Scope2)>
            ExpressionScopeConflictBecauseOfChildren
                = new(
                    "LAMA0241",
                    Error,
                    "Execution scope mismatch in the expression `{0}`: the sub-expression `{1}` is {2}, but the other sub-expression `{3}` is {4}.",
                    "Execution scope mismatch in an expression because two sub-expressions have a different execution scope.",
                    _category );

        internal static readonly DiagnosticDefinition<(string ParentExpression, string ParentScope, string ChildExpression, string ChildScope)>
            ExpressionScopeConflictBecauseOfParent
                = new(
                    "LAMA0242",
                    Error,
                    "Execution scope mismatch in the expression `{0}`: the expression is {1}, but the sub-expression `{2}` is {3}",
                    "Execution scope mismatch in an expression because a sub-expression has a different execution scope than the parent expression.",
                    _category );

        internal static readonly DiagnosticDefinition<(INamedTypeSymbol Type, string TypeScope, INamedTypeSymbol BaseType, string BaseTypeScope)>
            BaseTypeScopeConflict
                = new(
                    "LAMA0244",
                    Error,
                    "Execution scope mismatch: the type '{0}' is {1}, but the base type '{2}' is {3}.",
                    "Execution scope mismatch: mismatch between the run-time or compile-time nature of the declaration and its type arguments.",
                    _category );

        internal static readonly DiagnosticDefinition<ISymbol> UnexplainedTemplatingScopeConflict
            = new(
                "LAMA0245",
                "The syntax is invalid because it combines run-time and compile-time elements.",
                "'{0}' is invalid because it combines run-time and compile-time elements.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<None> CannotUseDynamicTypingInLocalFunction
            = new(
                "LAMA0246",
                "Signatures of Local function in templates cannot use dynamic typing",
                "The return type or parameter type of a local function in a template cannot be dynamic.",
                _category,
                Error );

        internal static readonly
            DiagnosticDefinition<(string AspectName, IDeclaration TargetDeclaration, IUserExpression Expression, IType ReturnType, IType DesiredType)>
            CannotConvertProceedReturnToType
                = new(
                    "LAMA0247",
                    "Cannot convert the actual return type of the Proceed method to the desired type",
                    "Cannot apply the aspect '{0}' to '{1}': cannot convert the result of '{2}', of type '{3}', to the desired type '{4}'.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(ISymbol Declaration, string Scope)> UnsafeCodeForbiddenInCompileTimeCode
            = new(
                "LAMA0248",
                "Compile-time code cannot contain unsafe code",
                "'{0}' cannot contain unsafe code because it is {1}.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<ISymbol> UnsafeCodeForbiddenInTemplate
            = new(
                "LAMA0249",
                "Template code cannot contain unsafe code",
                "'{0}' cannot contain unsafe code because it is a template.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<None> ForbiddenDynamicUseInTemplate
            = new(
                "LAMA0250",
                "Template code cannot use dynamic like this",
                "This use of 'dynamic' is not allowed in a template.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> DynamicVariableSetToNonDynamic
            = new(
                "LAMA0251",
                "Dynamic variables cannot be set to non-dynamic values.",
                "Dynamic variable '{0}' cannot be set to a non-dynamic value.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<ISymbol> PartialTemplatesForbidden
            = new(
                "LAMA0252",
                "Templates cannot be partial",
                "'{0}' cannot be partial because it is a template.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(ITypeSymbol, string)> CompileTimeTypeInInvocationOfRuntimeMethod
            = new(
                "LAMA0253",
                "Compile-time-only types cannot be used in invocations of run-time methods.",
                "Compile-time-only type '{0}' cannot be used in the invocation of run-time method '{1}'.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> OnlyLiteralArgumentInConfigureAwaitAfterProceedAsync
            = new(
                "LAMA0254",
                "ConfigureAwait after ProceedAsync has to have literal argument.",
                "The argument of ConfigureAwait after ProceedAsync can only be 'true' or 'false', it can't be '{0}'.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string Expression, string Type)> CannotCastRunTimeExpressionToCompileTimeType
            = new(
                "LAMA0255",
                "Cannot cast a run-time expression to a compile-time type.",
                "Cannot cast the run-time expression '{0}' to the compile-time type '{1}'.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> DynamicArgumentMustBeCastToIExpression
            = new(
                "LAMA0256",
                "The dynamic argument must be explicitly cast to IExpression.",
                "The dynamic expression '{0}' must be explicitly cast to 'IExpression' because it is a dynamic argument of a compile-time method that does not return a dynamic type.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> CannotSetTemplateMemberFromAttribute
            = new(
                "LAMA0257",
                "Cannot set a template member from an attribute.",
                "Cannot set a template member {0} from an attribute.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<ISymbol> CannotMarkDeclarationAsTemplate
            = new(
                "LAMA0258",
                "Declaration is an invalid declaration to be marked as a template.",
                "'{0}' is an invalid declaration to be marked as a template.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string Expression, string Left, string Right)> ExpressionScopeConflictInConditionalAccess
            = new(
                "LAMA0259",
                "Execution scope mismatch in conditional access expression.",
                "The null-conditional operator cannot be used in the expression '{0}', because '{1}' is compile-time, but '{2}' is run-time. Consider using a separate null-checking 'if' statement instead of the null-conditional operator.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<ISymbol> CompileTimeTypesCannotHaveTypeFabrics
            = new(
                "LAMA0260",
                "Type fabrics cannot be nested in compile-time types.",
                "The type fabric '{0}' cannot be nested in a compile-time type.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(ISymbol Declaration1, INamedTypeSymbol Attribute1, ISymbol Declaration2, INamedTypeSymbol Attribute2)>
            MultipleAdviceAttributes
                = new(
                    "LAMA0261",
                    "Declarations cannot have more than one template or advice attribute applied.",
                    "Multiple template or advice attributes found on the same declaration: {1} on {0} and {3} on {2}.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(ISymbol AccessorDeclaration, INamedTypeSymbol Attribute, string ContainingMemberKind)>
            AdviceAttributeOnAccessor
                = new(
                    "LAMA0262",
                    "Accessors cannot have template or advice attributes applied.",
                    "Accessor '{0}' cannot have the '{1}' attribute. Add the attribute to the containing {2} instead.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<None> DynamicInLambdaUnsupported
            = new(
                "LAMA0263",
                "Lambdas or anonymous functions returning a dynamic type are not supported. Consider casting the result to IExpression. For void expressions, use a lambda statement.",
                "Lambdas or anonymous functions returning a dynamic type are not supported except. Consider casting the result to IExpression.  For void expressions, use a lambda statement.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string, ITypeSymbol)> CompileTimeTemplateParameterWithRunTimeType
            = new(
                "LAMA0264",
                "Compile-time template parameters cannot have run-time-only types.",
                "The compile-time template parameter '{0}' cannot have the run-time-only type '{1}'.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string, ITypeParameterSymbol)> StaticInterfaceMembersNotSupportedOnCompileTimeTemplateTypeParameters
            = new(
                "LAMA0265",
                "Accessing static interface members is not supported on compile-time template type parameters.",
                "Accessing the static interface member '{0}' is not supported on compile-time template type parameter '{1}'.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<IMethodSymbol> ExtensionMethodMethodGroupConversion
            = new(
                "LAMA0267",
                "Method group conversion for extension methods is not supported.",
                "Converting extension method '{0}' to a delegate using a method group conversion is currently not supported.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> CantResolveDeclaration
            = new(
                "LAMA0268",
                "Could not resolve declaration when looking for template attributes.",
                "Could not resolve declaration with id '{0}' when looking for template attributes. This can happen when multiple references contain a type that's part of the declaration signature.",
                _category,
                Warning );

        internal static readonly DiagnosticDefinition<ISymbol> AnonymousTypeDifferentScopes
            = new(
                "LAMA0269",
                "Anonymous type can't can't be used as both run-time and compile-time in the same template.",
                "The anonymous type '{0}' can't can't be used as both run-time and compile-time in the same template.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> SubtemplateCallCantBeSubexpression
            = new(
                "LAMA0270",
                "Template call cannot be part of another expression or statement.",
                "Template call '{0}' cannot be part of another expression or statement, it can only be done as a stand-alone statement.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<ISymbol> ExtensionMethodTemplateNotSupported
            = new(
                "LAMA0271",
                "Extension method templates are not supported.",
                "The template '{0}' is an extension method, which is not supported.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition RedundantReturnNotAllowed
            = new(
                "LAMA0272",
                Error,
                "Redundant return statements are not allowed in templates.",
                "Redundant return statement is not allowed in a template.",
                _category );

        internal static readonly DiagnosticDefinition<ISymbol> SubtemplatesHaveToBeInvoked
            = new(
                "LAMA0273",
                "When a template references another template, it has to be directly invoked.",
                "The template '{0}' has to be directly invoked.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(ISymbol DeclaredSymbol, INamedTypeSymbol ContainingType)> TemplatesHaveToBeInTemplateProvider
            = new(
                "LAMA0274",
                "Templates have to be contained in an aspect, fabric, or a type implementing ITemplateProvider.",
                "The template '{0}' is contained in '{1}', which is not an aspect, a fabric, or a type marked implementing ITemplateProvider.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> SubtemplateCallWithMissingArgumentsCantBeVirtual
            = new(
                "LAMA0275",
                "Template call that uses optional parameters currently can't be virtual.",
                "Template call '{0}' currently cannot be virtual and use optional parameters at the same time.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<ISymbol> SubtemplateCantHaveRunTimeTypeParameter
            = new(
                "LAMA0276",
                "Called template can't have run-time type parameters.",
                "Called template '{0}' can't have run-time type parameters.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(ISymbol, TypeSyntax)> SubtemplateCantBeCalledWithRunTimeTypeParameter
            = new(
                "LAMA0277",
                "A template can't be called with run-time type parameters.",
                "The template '{0}' can't be called with type argument '{1}', which contains run-time template type parameter.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<ISymbol> AspectCantBeStruct
            = new(
                "LAMA0278",
                "An aspect or can't be a value type.",
                "The aspect '{0}' can't be a value type.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<ISymbol> CantCallAbstractSubtemplate
            = new(
                "LAMA0279",
                "Abstract or empty template can't be called.",
                "The abstract or empty template '{0}' can't be called.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string Expression, string RunTimeCondition)> CannotSetCompileTimeExpressionInRunTimeConditionalBlock
            = new(
                "LAMA0280",
                "Cannot set a compile-time expression in a block whose execution depends on a run-time condition.",
                "Cannot set the compile-time expression '{0}' here because it is part of a block whose execution depends on the run-time condition '{1}'.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(ISymbol Attribute, ISymbol? Target)> AttributeNotAllowedOnCompileTimeCode
            = new(
                "LAMA0281",
                "Attribute is not allowed on compile-time code.",
                "The attribute '{0}' is not allowed on the compile-time declaration '{1}', because it wouldn't have the expected effect.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string? Aspect, string RequiredCSharpVersion, string TargetCSharpVersion, IMemberOrNamedType Template)>
            AspectUsesHigherCSharpVersion
                = new(
                    "LAMA0282",
                    "Aspect uses higher C# version than what is allowed in the project.",
                    "The aspect '{0}' uses features of C# {1}, but it is used in a project built with C# {2}. Consider specifying <LangVersion>{1}</LangVersion> in this project or removing newer language features from the template '{3}' and then specifying <MetalamaTemplateLanguageVersion> in the aspect project.",
                    _category,
                    Warning );

        internal static readonly DiagnosticDefinition<INamedTypeSymbol> NonRecordPrimaryConstructorsNotSupported
            = new(
                "LAMA0283",
                _category,
                "Compile-time type '{0}' uses non-record primary constructors which is not currently supported. " +
                "You should remove the parameter list from the type and use explicitly defined constructors instead.",
                Error,
                "Non-record primary constructors are not currently supported in compile-time code." );

        internal static readonly DiagnosticDefinition UnknownScopedAnonymousMethod
            = new(
                "LAMA0284",
                _category,
                "The scope of the anonymous method or lambda block cannot be determined. Use meta.RunTime or meta.CompileTime to resolve the ambiguity.",
                Error,
                "The scope of the anonymous method or lambda block cannot be determined. Use meta.RunTime or meta.CompileTime to resolve the ambiguity." );

        internal static readonly DiagnosticDefinition<ITypeSymbol> TemplateAttributeOnLocalFunction
            = new(
                "LAMA0285",
                "Template and scope attributes are not allowed on local functions.",
                "The '{0}' attribute is not allowed on a local function.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> CannotCastRunTimeExpressionToIExpression
            = new(
                "LAMA0286",
                "Cannot cast a non-dynamic run-time expression to IExpression.",
                "Cannot cast the non-dynamic run-time expression '{0}' to IExpression. Use ExpressionFactory.Capture.",
                _category,
                Error );
    }
}
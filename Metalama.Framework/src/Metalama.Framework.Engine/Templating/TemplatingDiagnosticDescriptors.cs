// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
                "The expression is expected to be of a different scope (run-time or compile-time).",
                "The expression '{0}' is {1}, but it is expected to be {2} because the expression appears in {3}.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> SplitVariables
            = new(
                "LAMA0105",
                "Compile-time and run-time local variables cannot be mixed in the same declaration.",
                "The local variables {0} cannot be declared in the same declaration because some of them are compile-time and others are run-time. " +
                "Split the declaration into two declarations: one for the compile-time variables and one for the run-time variables.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string VariableName, string RunTimeCondition)> CannotSetCompileTimeVariableInRunTimeConditionalBlock
            = new(
                "LAMA0108",
                "A compile-time variable declared outside a block that depends on a run-time condition cannot be set in that block.",
                "The compile-time variable '{0}' cannot be set here because the assignment is in a block whose execution depends on a run-time " +
                "condition ('{1}'), and the variable is not declared in that block. Move the assignment out of this block, or declare the variable " +
                "inside the block.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> UndeclaredRunTimeIdentifier
            = new(
                "LAMA0109",
                "The run-time identifier was not declared.",
                "The run-time identifier '{0}' was not declared in the template. This can be caused by an error in the template or by a defect in " +
                "Metalama.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string LoopKind, string RunTimeCondition)> CannotHaveCompileTimeLoopInRunTimeConditionalBlock
            = new(
                "LAMA0110",
                "Cannot have a compile-time loop in a block whose execution depends on a run-time condition.",
                "The compile-time '{0}' loop is not allowed here because it is part of a block whose execution depends on the run-time condition " +
                "'{1}'. Move the loop out of the run-time-conditional block or use a compile-time 'foreach' loop instead.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string AspectName, string AssemblyName)>
            CannotFindAspectInCompilation
                = new(
                    "LAMA0113",
                    "An aspect type defined in a referenced assembly cannot be found in the compilation.",
                    "The aspect or template provider type '{0}' defined in the assembly '{1}' cannot be found in the current compilation.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(IDeclaration TargetDeclaration, DeclarationKind TargetKind,
                FormattableString Explanation)>
            CannotUseThisInStaticContext
                = new(
                    "LAMA0114",
                    "Cannot reference 'this' from a static context.",
                    "Cannot reference 'this' in an advice applied to {1} '{0}' because {2}.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(IDeclaration Advice, string Expression, IDeclaration TargetDeclaration, DeclarationKind TargetKind,
                string MissingKind, string? AlternativeSuggestion)>
            MetaMemberNotAvailable
                = new(
                    "LAMA0115",
                    "Cannot use a meta member in the current context",
                    "The template '{0}' cannot use '{1}' when applied to the {3} '{2}' because no '{4}' is available in this context.{5}",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(ISymbol DeclaringSymbol, ISymbol ReferencedSymbol, TemplatingScope DeclaringScope)>
            CannotReferenceCompileTimeOnly
                = new(
                    "LAMA0117",
                    "Cannot reference a compile-time-only declaration in a non-compile-time-only declaration.",
                    "Cannot reference '{1}' in '{0}' because '{1}' is compile-time-only but '{0}' is {2}. Consider adding [CompileTime] to '{0}', or do " +
                    "not use '{1}' in '{0}'.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(ISymbol DeclaringSymbol, ISymbol ReferencedSymbol, TemplatingScope DeclaringScope)>
            CannotReferenceCompileTimeOnlyRoslyn
                = new(
                    "LAMA0291",
                    "Cannot reference a compile-time-only Roslyn type in a non-compile-time-only declaration.",
                    "Cannot reference '{1}' in '{0}' because '{1}' is compile-time-only but '{0}' is {2}. Roslyn types are compile-time-only because " +
                    "the MSBuild property 'MetalamaRoslynIsCompileTimeOnly' is set to true, which is the default when the project references the " +
                    "Metalama.Framework.Sdk package. Consider adding [CompileTime] to '{0}', or set " +
                    "<MetalamaRoslynIsCompileTimeOnly>false</MetalamaRoslynIsCompileTimeOnly> in your project file.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<ISymbol>
            CompileTimeTypeNeedsRebuild
                = new(
                    "LAMA0118",
                    "The compile-time type must be rebuilt.",
                    "The compile-time type '{0}' has been modified since the last build. Metalama will stop analyzing this solution until the next " +
                    "build, and you may get errors caused by the absence of generated code. To resume analysis, finish the work on all compile-time " +
                    "logic, then build the project (even if the run-time code still has issues).",
                    _category,
                    Warning );

        internal static readonly DiagnosticDefinition<(ISymbol Declaration, string Namespace, string AttributeName)>
            CompileTimeCodeNeedsNamespaceImport
                = new(
                    "LAMA0119",
                    "A file that contains compile-time code does not import a Metalama.Framework namespace.",
                    "The compile-time declaration '{0}' is in a file that does not have a using directive for the '{1}' namespace or one of its main " +
                    "child namespaces. This may cause an inconsistent design-time experience. Add a using directive such as 'using {1}.Aspects;' to the " +
                    "file.",
                    _category,
                    Warning );

        internal static readonly DiagnosticDefinition<(ISymbol ReferencedDeclaration, ISymbol ReferencingDeclaration)>
            OnlyMethodsCanBeSubtemplates
                = new(
                    "LAMA0220",
                    "A template can only reference other templates that are methods.",
                    "The template '{0}' cannot be referenced from the template '{1}' because it is not a method. A template can only call other " +
                    "templates that are methods.",
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
                    "Cannot use a Proceed variant that is not compatible with the return type of the target method.",
                    "Cannot use 'meta.{0}()' in '{1}' because the return type of '{1}' is not compatible with 'meta.{0}()'. Use 'meta.Proceed()', which " +
                    "supports any return type.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<string>
            CannotUseDynamicInUninitializedLocal
                = new(
                    "LAMA0224",
                    "Cannot declare a local variable of type 'dynamic' without an initializer.",
                    "The 'dynamic' keyword cannot be used in the local variable '{0}' because it is not initialized. Initialize the variable in its " +
                    "declaration, or use 'meta.DefineLocalVariable' to declare a run-time variable without an initializer.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<string>
            CannotUseDynamicWithTargetTypedExpression
                = new(
                    "LAMA0225",
                    "Cannot initialize a dynamic-typed variable with a target-typed expression.",
                    "The 'dynamic' keyword cannot be used in the local variable '{0}' because it is initialized with a target-typed expression (such as " +
                    "'default' or 'null'). Initialize the variable with an expression of type 'dynamic', for example 'meta.Default( type )'.",
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
                "'dynamic' cannot be used as a generic argument, an array element type, a tuple element type, or a ref type in a template.",
                "The type '{0}' is forbidden in a template: 'dynamic' cannot be used as a generic argument type, an array element type, a tuple element type or a ref type.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<ITypeSymbol> NeutralTypesForbiddenInNestedRunTimeTypes
            = new(
                "LAMA0229",
                "Types that are both compile-time and run-time are forbidden in run-time-only types.",
                "The type '{0}' cannot be both run-time and compile-time, like an aspect class or a [RunTimeOrCompileTime] class, because it is " +
                "nested in a run-time-only type. Move it out of the containing type.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<ITypeSymbol> NestedCompileTypesMustBePrivate
            = new(
                "LAMA0230",
                "Nested compile-time types must have private accessibility.",
                "The compile-time type '{0}' must be private because it is nested in a run-time type.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(ITypeSymbol NestedType, Type TypeFabric)> RunTimeTypesCannotHaveCompileTimeTypesExceptTypeFabrics
            = new(
                "LAMA0231",
                "Compile-time types cannot be nested in run-time types, except for type fabrics.",
                "The compile-time type '{0}' cannot be nested in a run-time type. The only compile-time types that can be nested in a run-time type " +
                "are classes derived from '{1}'. Move '{0}' out of the containing type.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> TemplateUsesUnsupportedLanguageVersion
            = new(
                "LAMA0232",
                "Template code uses a feature of a C# version later than the template language version.",
                "Template code must be compatible with C# {0}, but this syntax requires a later version of C#.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(ISymbol DeclaringSymbol, ISymbol ReferencedSymbol, string? Explanation)>
            CannotUseTemplateOnlyOutOfTemplate
                = new(
                    "LAMA0233",
                    "Cannot use a template-only member outside of a template.",
                    "Cannot use '{1}' in '{0}' because it is only allowed inside a template.{2}",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<ISymbol> PartiallyUnresolvedSymbolInTemplate
            = new(
                "LAMA0235",
                "A type or member used in a template refers to a type that cannot be resolved.",
                "The type or member '{0}' refers to a type that cannot be resolved. Fix this error first, otherwise Metalama may report irrelevant " +
                "errors in the current template.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(ISymbol DeclaringSymbol, ISymbol ReferencedSymbol, TemplatingScope DeclaringSymbolScope)>
            CannotReferenceRunTimeOnly
                = new(
                    "LAMA0236",
                    "Cannot reference a run-time-only declaration in code that can execute at compile time.",
                    "Cannot reference '{1}' in '{0}' because '{1}' is run-time-only but '{0}' is {2}. Run-time-only declarations can be referenced only " +
                    "in templates and in run-time code.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<ISymbol>
            AbstractTemplateCannotHaveRunTimeSignature
                = new(
                    "LAMA0237",
                    "An abstract template property cannot have a run-time-only type.",
                    "The template property '{0}' cannot be abstract because its type or the type of one of its parameters is run-time-only. Make the " +
                    "template property virtual and give it a default implementation.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(ISymbol Member, INamedTypeSymbol DeclaringType, TemplatingScope DeclaringTypeScope)>
            OnlyNamedTemplatesCanHaveDynamicSignature
                = new(
                    "LAMA0238",
                    "Only templates and members of run-time-only types can use the 'dynamic' type.",
                    "'{0}' cannot use the 'dynamic' type because it is not a template and its declaring type '{1}' is {2}. Only templates and members " +
                    "of run-time-only types can use the 'dynamic' type.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(string ParentExpression, string Expression1, string Scope1, string Expression2, string Scope2)>
            ExpressionScopeConflictBecauseOfChildren
                = new(
                    "LAMA0241",
                    Error,
                    "Execution scope mismatch in the expression '{0}': the sub-expression '{1}' is {2}, but the other sub-expression '{3}' is {4}.",
                    "Execution scope mismatch in an expression because two sub-expressions have a different execution scope.",
                    _category );

        internal static readonly DiagnosticDefinition<(string ParentExpression, string ParentScope, string ChildExpression, string ChildScope)>
            ExpressionScopeConflictBecauseOfParent
                = new(
                    "LAMA0242",
                    Error,
                    "Execution scope mismatch in the expression '{0}': the type of the expression is {1}, but the sub-expression '{2}' is {3}.",
                    "Execution scope mismatch in an expression because a sub-expression has a different execution scope than the parent expression.",
                    _category );

        internal static readonly DiagnosticDefinition<(INamedTypeSymbol Type, string TypeScope, INamedTypeSymbol BaseType, string BaseTypeScope)>
            BaseTypeScopeConflict
                = new(
                    "LAMA0244",
                    Error,
                    "Execution scope mismatch: the type '{0}' is {1}, but its base type or interface '{2}' is {3}.",
                    "Execution scope mismatch between a type and its base type or interface.",
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
                "The signature of a local function in a template cannot use dynamic typing.",
                "The return type or a parameter type of a local function in a template cannot be dynamic. Use a specific type, for instance " +
                "'object', instead.",
                _category,
                Error );

        internal static readonly
            DiagnosticDefinition<(string AspectName, IDeclaration TargetDeclaration, IUserExpression Expression, IType ReturnType, IType DesiredType)>
            CannotConvertProceedReturnToType
                = new(
                    "LAMA0247",
                    "A template cannot return a void expression from a method or local function that has a non-void return type.",
                    "Cannot apply the aspect '{0}' to '{1}': the template returns '{2}', of type '{3}', from a method or local function whose return " +
                    "type is '{4}'.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(ISymbol Declaration, string Scope)> UnsafeCodeForbiddenInCompileTimeCode
            = new(
                "LAMA0248",
                "Compile-time code cannot contain unsafe code.",
                "'{0}' cannot contain unsafe code because it is {1} code.",
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
                "The 'dynamic' type cannot be used in this context in a template.",
                "In a template, 'dynamic' cannot be used as the type of a cast, 'as', 'is', or 'default' expression, as the type of a lambda " +
                "parameter or return value, or as a type argument of a method.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> DynamicVariableSetToNonDynamic
            = new(
                "LAMA0251",
                "A 'dynamic' variable must be initialized with a 'dynamic' expression.",
                "The variable '{0}' is declared as 'dynamic', so it must be initialized with an expression of type 'dynamic'. Declare the variable " +
                "with 'var' or with a non-dynamic type.",
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
                "The argument of ConfigureAwait after ProceedAsync must be a literal.",
                "The argument of 'ConfigureAwait' after 'meta.ProceedAsync()' must be the literal 'true' or 'false', but it is '{0}'. To choose the " +
                "value at compile time, use a compile-time 'if' statement with a literal argument in each branch.",
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
                "A template member cannot be set from an aspect custom attribute.",
                "The template member '{0}' cannot be set from an aspect custom attribute. Add a separate aspect property that is not a template and " +
                "set this property instead.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<ISymbol> CannotMarkDeclarationAsTemplate
            = new(
                "LAMA0258",
                "The declaration cannot be a template.",
                "'{0}' cannot be a template because constructors, finalizers, operators, and conversion operators are not supported as templates. " +
                "Introduce the member programmatically from a template method instead.",
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
                    "Only one template or advice attribute is allowed on a declaration, the member it overrides, and its containing property or event, " +
                    "but '{1}' is applied to '{0}' and '{3}' is applied to '{2}'.",
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
                "Expression-bodied lambdas whose expression is of type 'dynamic' are not supported.",
                "Expression-bodied lambdas whose expression is of type 'dynamic' are not supported. Cast the expression to a non-dynamic type, for " +
                "example 'object' or 'IExpression'. Alternatively, use a local function, or, for a void expression, a lambda with a block body.",
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
                "Accessing the static interface member '{0}' is not supported on the compile-time template type parameter '{1}'. Call a run-time " +
                "generic method that accesses the member instead, or make the type parameter run-time.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<IMethodSymbol> ExtensionMethodMethodGroupConversion
            = new(
                "LAMA0267",
                "Method group conversion for extension methods is not supported.",
                "Converting the extension method '{0}' to a delegate using a method group conversion is not supported in templates. Use a lambda " +
                "expression that invokes the method instead.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> CantResolveDeclaration
            = new(
                "LAMA0268",
                "Could not resolve a declaration when looking for template attributes.",
                "Could not resolve the declaration with id '{0}' when looking for template attributes. This can happen when several referenced " +
                "assemblies define a type that is part of the declaration signature.",
                _category,
                Warning );

        internal static readonly DiagnosticDefinition<ISymbol> AnonymousTypeDifferentScopes
            = new(
                "LAMA0269",
                "An anonymous type cannot be used in both run-time and compile-time code in the same template.",
                "The anonymous type '{0}' cannot be used in both run-time and compile-time code in the same template. Change the property names or " +
                "types of one of the anonymous objects, or use a named type instead.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> SubtemplateCallCantBeSubexpression
            = new(
                "LAMA0270",
                "Template call cannot be part of another expression or statement.",
                "The template call '{0}' cannot be part of another expression or statement. A template can only be called as a stand-alone " +
                "statement.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<ISymbol> ExtensionMethodTemplateNotSupported
            = new(
                "LAMA0271",
                "A template cannot be an extension method.",
                "The template '{0}' cannot be an extension method. To introduce an extension method, mark the first parameter of the template with " +
                "the [This] attribute, or set the IParameterBuilder.IsThis property programmatically.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition RedundantReturnNotAllowed
            = new(
                "LAMA0272",
                Error,
                "Redundant 'return' statements are not allowed in templates. Remove this 'return' statement.",
                "Redundant return statement is not allowed in a template.",
                _category );

        internal static readonly DiagnosticDefinition<ISymbol> SubtemplatesHaveToBeInvoked
            = new(
                "LAMA0273",
                "A template can only be referenced in a direct call.",
                "The template '{0}' can only be referenced in a direct call. It cannot be assigned to a delegate, passed as an argument, or called " +
                "with the '?.' operator.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(ISymbol DeclaredSymbol, INamedTypeSymbol ContainingType)> TemplatesHaveToBeInTemplateProvider
            = new(
                "LAMA0274",
                "Templates have to be contained in an aspect, fabric, or a type implementing ITemplateProvider.",
                "The template '{0}' is contained in '{1}', which is not an aspect, a fabric, or a type implementing ITemplateProvider.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<string> SubtemplateCallWithMissingArgumentsCantBeVirtual
            = new(
                "LAMA0275",
                "A call to a virtual template cannot omit arguments of optional parameters.",
                "The template call '{0}' omits arguments of optional parameters, which is not supported when the called template is virtual, " +
                "abstract, or an override. Specify all arguments, or make the template non-virtual.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<ISymbol> SubtemplateCantHaveRunTimeTypeParameter
            = new(
                "LAMA0276",
                "A called template cannot have run-time type parameters.",
                "The called template '{0}' has a run-time type parameter. The type parameters of a called template must be marked with " +
                "[CompileTime].",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(ISymbol, TypeSyntax)> SubtemplateCantBeCalledWithRunTimeTypeParameter
            = new(
                "LAMA0277",
                "A template cannot be called with a type argument that contains a run-time type parameter.",
                "The template '{0}' cannot be called with the type argument '{1}' because this type argument contains a run-time type parameter of " +
                "the calling template.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<ISymbol> AspectCantBeStruct
            = new(
                "LAMA0278",
                "An aspect cannot be a value type.",
                "The aspect '{0}' cannot be a value type. Declare it as a class.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<ISymbol> CantCallAbstractSubtemplate
            = new(
                "LAMA0279",
                "An abstract or empty template cannot be called.",
                "The template '{0}' cannot be called because it is abstract or marked with [Template(IsEmpty = true)]. Call a template that has an " +
                "implementation instead, for example a virtual template with a default implementation.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string Expression, string RunTimeCondition)> CannotSetCompileTimeExpressionInRunTimeConditionalBlock
            = new(
                "LAMA0280",
                "A compile-time expression cannot be set in a block whose execution depends on a run-time condition.",
                "The compile-time expression '{0}' cannot be set here because the assignment is in a block whose execution depends on a run-time " +
                "condition ('{1}'). In such a block, only compile-time local variables declared in the block can be set.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(ISymbol Attribute, ISymbol? Target)> AttributeNotAllowedOnCompileTimeCode
            = new(
                "LAMA0281",
                "Attribute is not allowed on compile-time code.",
                "The attribute '{0}' is not allowed on the compile-time declaration '{1}', because it would not have the expected effect.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(string? Aspect, string RequiredCSharpVersion, string TargetCSharpVersion, IMemberOrNamedType Template)>
            AspectUsesHigherCSharpVersion
                = new(
                    "LAMA0282",
                    "The aspect uses a higher C# version than the project allows.",
                    "The aspect '{0}' uses features of C# {1}, but it is used in a project built with C# {2}. Consider specifying <LangVersion>{1}</LangVersion> in this project or removing newer language features from the template '{3}' and then specifying <MetalamaTemplateLanguageVersion> in the aspect project.",
                    _category,
                    Warning );

        internal static readonly DiagnosticDefinition<INamedTypeSymbol> NonRecordPrimaryConstructorsNotSupported
            = new(
                "LAMA0283",
                _category,
                "The compile-time type '{0}' has a primary constructor, which is not supported in compile-time code. Remove the parameter list from " +
                "the type and declare an explicit constructor instead.",
                Error,
                "Non-record primary constructors are not currently supported in compile-time code." );

        internal static readonly DiagnosticDefinition UnknownScopedAnonymousMethod
            = new(
                "LAMA0284",
                _category,
                "The scope of the anonymous method or lambda expression with a block body cannot be determined. Use 'meta.RunTime' or " +
                "'meta.CompileTime' to resolve the ambiguity.",
                Error,
                "The scope of an anonymous method or lambda expression with a block body cannot be determined." );

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

        internal static readonly DiagnosticDefinition<(IDeclaration TargetDeclaration, DeclarationKind TargetKind, FormattableString Explanation)>
            NoReceiverInCurrentContext
                = new(
                    "LAMA0287",
                    "The current context has no receiver",
                    "Cannot get a receiver in an advice applied to {1} '{0}' because {2}.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(string Expression, string RunTimeCondition)>
            CannotCallCompileTimeMethodWithSideEffectsInRunTimeConditionalBlock
                = new(
                    "LAMA0288",
                    "A compile-time expression with side effects cannot be used as a statement in a block that depends on a run-time condition.",
                    "The compile-time expression '{0}' cannot be used as a statement here because the statement is in a block whose execution depends " +
                    "on a run-time condition ('{1}'). A compile-time expression used as a statement is assumed to have side effects, and these side " +
                    "effects cannot depend on a run-time condition. Move the statement out of this block, or make the condition compile-time.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(string Expression, string CompileTimeExpression, string RunTimeMember)>
            CannotUseConditionalAccessWithCompileTimeToRunTimeMember
                = new(
                    "LAMA0289",
                    "The null-conditional operator cannot be used to access a run-time member on a compile-time expression.",
                    "The null-conditional operator cannot be used in the expression '{0}' because '{1}' is compile-time and '.{2}' "
                    + "returns a run-time value. When the compile-time expression is null, its type is unknown, so type-preserving "
                    + "run-time code cannot be generated. Use a compile-time null check (e.g. an 'if' statement) instead of the '?.' operator.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<string> DuplicateAspectTypeInCompilation
            = new(
                "LAMA0290",
                "Multiple aspect types with the same name were found in the compilation.",
                "The aspect type '{0}' was found in two different versions of the same referenced assembly. Make sure that all references to this " +
                "assembly use the same version.",
                _category,
                Error );

        internal static readonly DiagnosticDefinition<(ISymbol Member, string MemberScope, ISymbol DeclaringType, string DeclaringTypeScope)>
            MemberScopeIncompatibleWithDeclaringType
                = new(
                    "LAMA0292",
                    Error,
                    "Execution scope mismatch: the member '{0}' is {1}, but the declaring type '{2}' is {3}. Change the scope of the declaring type, " +
                    "for example with the [CompileTime] or [RunTimeOrCompileTime] attribute, or move the member to a type of a compatible scope.",
                    "Execution scope mismatch: the scope of a member is not compatible with the scope of its declaring type.",
                    _category );

        internal static readonly DiagnosticDefinition<(string AspectName, IDeclaration TargetMethod)>
            CannotUseNormalTemplateWithTryCatchOnAsyncIterator
                = new(
                    "LAMA0293",
                    "Cannot apply a template that contains a try-catch block to an async iterator method.",
                    "The aspect '{0}' cannot override the async iterator method '{1}' because the expanded template contains "
                    + "'yield return' inside a try block with a catch clause, which is not allowed by C#. "
                    + "Use a dedicated async iterator template or restructure the template to avoid wrapping 'meta.Proceed()' in a try-catch block.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<(string Feature, ISymbol Declaration)>
            LanguageFeatureNotSupportedInCompileTimeCode
                = new(
                    "LAMA0294",
                    "The C# language feature is not supported in compile-time code.",
                    "The declaration '{1}' cannot use the C# feature '{0}', because compile-time code is compiled for "
                    + "netstandard2.0, which does not support this feature.",
                    _category,
                    Error );

        internal static readonly DiagnosticDefinition<string> CantResolveDeclarativeAdvice
            = new(
                "LAMA0295",
                "Could not resolve the declaration of a declarative advice.",
                "Could not resolve the declaration with id '{0}' while preparing the declarative advice of an aspect. That advice is ignored. "
                + "This can happen when the declaration is absent from the compilation that the aspect runs against, or when several "
                + "references of the compilation contain a type that is part of the signature of the declaration.",
                _category,
                Error );
    }
}
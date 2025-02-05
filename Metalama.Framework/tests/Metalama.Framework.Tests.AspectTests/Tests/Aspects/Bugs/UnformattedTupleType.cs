#if TEST_OPTIONS
// @TestUnformattedOutput
#endif

using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.UnformattedTupleType;

// <target>
public class Bug
{
    [TaskException]
    public Task<(int a, int b)> Method() => Task.FromResult( ( 1, 2 ) );
}

public class TaskExceptionAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        try
        {
            return meta.Proceed();
        }

        // ReSharper disable once EntityNameCapturedOnly.Local
        // ReSharper disable once InconsistentNaming
        catch (Exception __ex)
        {
            // Converts to TaskEx.FromException(ex) with generic support
            return ExpressionHelper.TaskExFromException( (INamedType)meta.Target.Method.ReturnType, nameof(__ex) ).Value;
        }
    }

    public override async Task<dynamic?> OverrideAsyncMethod() => await meta.ProceedAsync();
}

[CompileTime]
public static class ExpressionHelper
{
    /// <summary>
    ///     Produces the expression `Task.FromException({exception_name})` with generic support.
    /// </summary>
    public static IExpression TaskExFromException( INamedType returnType, string exceptionName )
        => new FromExceptionExpression( returnType, exceptionName ).ToExpression();

    private class FromExceptionExpression : IExpressionBuilder
    {
        public FromExceptionExpression( INamedType returnType, string exceptionName )
        {
            ReturnType = returnType;
            ExceptionName = exceptionName;
        }

        private INamedType ReturnType { get; }

        private string ExceptionName { get; }

        public IExpression ToExpression()
        {
            var builder = new ExpressionBuilder();

            builder.AppendVerbatim( "System.Threading.Tasks.Task.FromException" );

            if (ReturnType.IsGeneric)
            {
                builder.AppendVerbatim( "<" );

                // This worked in 2024.0
                // It seems like AppendTypeName doesn't like Tuples anymore and is missing a space between the type and name.
                // i.e. (int a, int b) becomes (inta, intb).
                builder.AppendTypeName( ReturnType.TypeArguments[0] );
                builder.AppendVerbatim( ">" );
            }

            builder.AppendVerbatim( "(" );
            builder.AppendVerbatim( ExceptionName );
            builder.AppendVerbatim( ")" );

            return builder.ToExpression();
        }
    }
}
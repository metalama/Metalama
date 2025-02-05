using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

/*
 * #30759 Aspect linker: LinkerLinkingStep.CleanupBodyRewriter throws ArgumentOutOfRangeException
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug30840
{
    public class LogExceptionContextAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var __metalma_currentThreadId = Thread.CurrentThread.ManagedThreadId;
            var __metalama_result = new StringBuilder();

            __metalama_result.Append( meta.Target.Type.ToDisplayString( CodeDisplayFormat.DiagnosticMessage ) );
            __metalama_result.Append( "." );
            __metalama_result.Append( meta.Target.Method.Name );
            __metalama_result.Append( "(" );

            // var __metalama_i = meta.CompileTime(0);
            if (meta.Target.Parameters.Count > 0)
            {
                var _metalama_firstParam = meta.Target.Parameters[0];

                foreach (var __metalama_p in meta.Target.Parameters)
                {
                    var __metalama_comma = __metalama_p != _metalama_firstParam ? ", " : "";

                    if (__metalama_p.RefKind == RefKind.Out)
                    {
                        __metalama_result.Append( $"{__metalama_comma}{__metalama_p.Name} = <out> " );
                    }
                    else
                    {
                        __metalama_result.Append( $"{__metalama_comma}{__metalama_p.Name} = {{" );
                        var __metalama_json = string.Empty;

                        try
                        {
                            __metalama_json = JsonConvert.SerializeObject( __metalama_p.Value );
                        }
                        catch
                        {
                            var __metalama_temp = new InterpolatedStringBuilder();
                            __metalama_temp.AddExpression( __metalama_p.Value );
                            __metalama_json = __metalama_temp.ToValue();
                        }

                        __metalama_result.Append( __metalama_json );
                        __metalama_result.Append( "}" );
                    }

                    //     __metalama_i++;
                }
            }

            __metalama_result.Append( ")" );

            ParamsStacks.Push( __metalma_currentThreadId, __metalama_result.ToString() );

            try
            {
                var result = meta.Proceed();

                return result;
            }
            finally
            {
                ParamsStacks.Pop( __metalma_currentThreadId );
            }
        }
    }

    internal static class ParamsStacks
    {
        public static void Push( int threadId, string xml ) { }

        public static void Pop( int threadId ) { }
    }

    public interface ILogger
    {
        void WriteInfo( string v );
    }

    public interface IEntityWriter { }

    public interface ICommandExecutionContext { }

    public class Response { }

    public class OrderResponse : Response { }

    public class OrderCommand { }

    public class AsyncResponseProcessor<T, TCommand> where TCommand : OrderCommand
    {
        public AsyncResponseProcessor( IEntityWriter writer ) { }

        protected virtual async Task HandleFailureAsync( OrderResponse response, ICommandExecutionContext context )
        {
            await Task.Yield();
        }
    }

    // <target>
    public class OrderAsyncResponseProcessor<TCommand> : AsyncResponseProcessor<Response, TCommand> where TCommand : OrderCommand
    {
        private readonly ILogger _logger;

        public OrderAsyncResponseProcessor( IEntityWriter writer, ILogger logger ) : base( writer )
        {
            _logger = logger;
        }

        [LogExceptionContextAspect]
        protected override async Task HandleFailureAsync( OrderResponse response, ICommandExecutionContext context )
        {
            _logger.WriteInfo( $"Handling failure response" );
            await base.HandleFailureAsync( response, context );
            _logger.WriteInfo( $"Handled failure response " );
        }
    }
}
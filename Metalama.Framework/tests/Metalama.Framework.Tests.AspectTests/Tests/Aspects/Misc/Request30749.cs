using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Code;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Request30749
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
                            __metalama_json = /* Newtonsoft.Json.JsonConvert.SerializeObject(__metalama_p.Value) */ "";
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
                return meta.Proceed();
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

    public class OrderCommand { }

    public interface ILogger
    {
        void WriteInfo( string msg );
    }

    public class BaseProcessor
    {
        protected virtual Task HandleFailureAsync( object response, object context ) => Task.CompletedTask;
    }

    // <target>
    public class OrderAsyncResponseProcessor<TCommand> : BaseProcessor where TCommand : OrderCommand
    {
        private readonly ILogger _logger = null!;

        [LogExceptionContextAspect]
        protected override async Task HandleFailureAsync( object response, object context )
        {
            _logger.WriteInfo( $"Handling failure response" );
            await base.HandleFailureAsync( response, context );
            _logger.WriteInfo( $"Handled failure response " );
        }
    }
}
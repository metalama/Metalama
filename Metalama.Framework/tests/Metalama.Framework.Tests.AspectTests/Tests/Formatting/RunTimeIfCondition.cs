using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Highlighting.IfStatements.RunTimeIfCondition
{
    class RunTimeClass
    {
        public void RunTimeMethod()
        {
        }
    }

    [CompileTime]
    class CompileTimeClass
    {
        public void CompileTimeMethod()
        {
        }
    }

    class Aspect : IAspect
    {
        [Template]
        dynamic? Template()
        {
            var runTimeObject = new RunTimeClass();
            var compileTimeObject = new CompileTimeClass();

            int runTimeVariable = 1;

            if (runTimeVariable == 1)
            {
                runTimeObject.RunTimeMethod();
                compileTimeObject.CompileTimeMethod();
                meta.Target.Method.ToString();
            }
            else
            {
                runTimeObject.RunTimeMethod();
                compileTimeObject.CompileTimeMethod();
                meta.Target.Method.ToString();
            }

            return meta.Proceed();
        }
    }
}

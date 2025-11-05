#:package Metalama.Framework

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

[AttributeUsage(AttributeTargets.Method)]
public sealed class LogAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var method = $"{meta.Target.Method.DeclaringType!.Name}.{meta.Target.Method.Name}";
        Console.WriteLine($"→ {method}");

        try
        {
            var result = meta.Proceed();

            if (meta.Target.Method.ReturnType.Equals(typeof(void)))
            {
                Console.WriteLine($"← {method} completed.");
                return null;
            }
            else
            {
                Console.WriteLine($"← {method} returned: {result}");
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✖ {method} threw {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }
}

public static class Calculator
{
    [Log] public static int Add(int a, int b) => a + b;
    [Log] public static int Divide(int a, int b) => a / b;
    [Log] public static void Ping() { }
}

public class Program
{
    public static void Main()
    {
        Console.WriteLine("Running Metalama aspect demo…");
        Console.WriteLine($"Add(2,3) = {Calculator.Add(2,3)}");

        try { Calculator.Divide(10,0); } catch { /* already logged */ }

        Calculator.Ping();
        Console.WriteLine("Done.");
    }
}


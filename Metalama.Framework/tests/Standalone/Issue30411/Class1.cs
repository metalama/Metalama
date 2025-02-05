#if MY_FLAG
System.Console.WriteLine("Hello!");
#else
#error MY_FLAG is not defined.
#endif
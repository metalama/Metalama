using XenoAtom.Logging;
using XenoAtom.Logging.Writers;

namespace Issue1531;

public partial class Program
{
    public static Logger Logger { get; internal set; }

    [LogFormatter("[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level}] [{LoggerName}] {Text}")]
    public static partial LogFormatter MyLogFormatter { get; }

    static void Main(string[] args)
    {
        LogManager.Initialize(new()
        {
            RootLogger =
            {
                MinimumLevel = LogLevel.Info,
                Writers =
                {
                    new StreamLogWriter(Console.OpenStandardOutput())
                    {
                        Formatter = MyLogFormatter with
                        {
                            LevelFormat = LogLevelFormat.Long,
                        }
                    }
                }
            }
        });
        Logger = LogManager.GetLogger("Program");
        SayHello();
    }

    [LogAspect]
    static void SayHello()
    {
        Logger.Info("Hello, World!");
    }
}

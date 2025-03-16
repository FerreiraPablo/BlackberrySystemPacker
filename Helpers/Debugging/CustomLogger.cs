using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackberrySystemPacker.Helpers.Debugging
{
    internal class CustomLogger: ILogger
    {
        private readonly string _categoryName;
        
        private readonly LogLevel _logLevel;

        public CustomLogger(string categoryName, LogLevel logLevel)
        {
            _categoryName = categoryName;
            _logLevel = logLevel;
        }
        
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
        
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _logLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
            return;
            }
            
            ConsoleColor originalColor = ConsoleColor.White;
            
            // Set color based on log level
            Console.ForegroundColor = logLevel switch
            {
            LogLevel.Trace => ConsoleColor.Gray,
            LogLevel.Debug => ConsoleColor.Blue,
            LogLevel.Information => ConsoleColor.Green,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.DarkRed,
            _ => originalColor
            };
            
            // Write just the log level with color
            Console.Write($"{logLevel}: ");
            
            // Restore original color, then write the message
            Console.ForegroundColor = originalColor;
            Console.WriteLine(formatter(state, exception));
        }
    }
}

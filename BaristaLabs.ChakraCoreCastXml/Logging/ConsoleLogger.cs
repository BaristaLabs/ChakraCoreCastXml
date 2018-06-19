namespace BaristaLabs.ChakraCoreCastXml.Logging
{
    using System;
    using System.IO;

    /// <summary>
    /// Default logger to Console.Out.
    /// </summary>
    public class ConsoleLogger : LoggerBase
    {
        public ConsoleLogger()
        {
            Output = Console.Out;
        }

        /// <summary>
        /// Gets or sets the output <see cref="TextWriter"/>. Default is set to <see cref="Console.Out"/>.
        /// </summary>
        /// <value>The output <see cref="TextWriter"/>.</value>
        public TextWriter Output { get; set; }

        /// <summary>
        /// Exits the process with the specified reason.
        /// </summary>
        /// <param name="reason">The reason.</param>
        /// <param name="exitCode">The exit code</param>
        public override void Exit(string reason, int exitCode)
        {
            if (Output == null)
                return;

            Log(LogLevel.Error, LogLocation.EmptyLocation, "", "", "Process stopped. " + reason, null);
            Environment.Exit(exitCode);
        }

        /// <summary>
        /// Logs the specified log message.
        /// </summary>
        /// <param name="logLevel">The log level</param>
        /// <param name="logLocation">The log location.</param>
        /// <param name="code">The code.</param>
        /// <param name="context">The context.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="parameters">The parameters.</param>
        public override void Log(LogLevel logLevel, LogLocation logLocation, string context, string code, string message, Exception exception, params object[] parameters)
        {
            lock (this)
            {
                if (Output == null)
                    return;

                string lineMessage = FormatMessage(logLevel, logLocation, context, message, exception, parameters);

                Output.WriteLine(lineMessage);
                Output.Flush();

                if (exception != null)
                    Output.WriteLine(exception.ToString());
            }
        }
    }
}

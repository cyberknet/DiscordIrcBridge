using Serilog.Sinks.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Logging
{
    public class SerilogHooks
    {
        public static SerilogFileCycleHook FileCycleHook => new SerilogFileCycleHook();
    }

    public class SerilogFileCycleHook : FileLifecycleHooks
    {
        public string CurrentLoggingFilePath { get; private set; }

        public override Stream OnFileOpened(string path, Stream underlyingStream, Encoding encoding)
        {
            CurrentLoggingFilePath = path;

            return base.OnFileOpened(path, underlyingStream, encoding);
        }
    }
}

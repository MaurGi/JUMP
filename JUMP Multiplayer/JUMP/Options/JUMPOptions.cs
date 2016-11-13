using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JUMP
{
    public enum LogLevels
    {
        Verbose = 3,
        Debug = 2,
        Info = 1,
        Error = 0,
    }

    public static class JUMPOptions
    {
        public static string GameVersion = "0.1";
        public static byte NumPlayers = 2;
        public static int DisconnectTimeout = 10 * 1000;
        public static int SnapshotsPerSec = 3;
        public static LogLevels LogLevel = LogLevels.Error;
        public static AuthenticationValues CustomAuth = null;

        static JUMPOptions()
        {
#if DEBUG
            DisconnectTimeout = 60 * 1000;
            LogLevel = LogLevels.Debug;
#endif
        }
    }
}

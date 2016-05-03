using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JUMP
{
    public static class JUMPOptions
    {
        public static string GameVersion = "0.1";
        public static byte NumPlayers = 2;
#if DEBUG
        public static int DisconnectTimeout = 60 * 1000;
#else
        public static int DisconnectTimeout = 10 * 1000;
#endif
        public static int SnapshotsPerSec = 3;
    }

}

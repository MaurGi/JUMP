using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JUMP
{
    public interface IJUMPPlayer
    {
        int PlayerID { get; set; }
        bool IsConnected { get; set; }
    }

    public class JUMPPlayer : IJUMPPlayer
    {
        public int PlayerID { get; set; }
        public bool IsConnected { get; set; }

        public JUMPPlayer()
        {
            IsConnected = false;
        }
    }
}

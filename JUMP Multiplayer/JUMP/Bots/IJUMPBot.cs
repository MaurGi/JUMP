using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JUMP
{
    public interface IJUMPBot : IJUMPPlayer
    {
        void Tick(double ElapsedSeconds);

        IJUMPGameServerEngine Engine { get; set; }
    }
}

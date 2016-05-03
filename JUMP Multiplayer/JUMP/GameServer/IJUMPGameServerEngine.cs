using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JUMP
{
    public interface IJUMPGameServerEngine
    {
        void StartGame(List<JUMPPlayer> Players);
        void Tick(double ElapsedSeconds);
        void ProcessCommand(JUMPCommand command);
        JUMPCommand CommandFromEvent(byte eventCode, object content);
        JUMPCommand_Snapshot TakeSnapshot(int ForPlayerID);
    }
}

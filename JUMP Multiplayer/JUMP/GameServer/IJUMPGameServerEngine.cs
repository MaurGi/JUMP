using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JUMP
{
    public interface IJUMPGameServerEngine
    {
        /// <summary>
        /// Called by JUMPServer when all clients have sent the connect command to the server
        /// </summary>
        /// <param name="Players">List of Players participating in the game</param>
        void StartGame(List<IJUMPPlayer> Players);
        void Tick(double ElapsedSeconds);
        void ProcessCommand(JUMPCommand command);
        JUMPCommand CommandFromEvent(byte eventCode, object content);
        JUMPCommand_Snapshot TakeSnapshot(int ForPlayerID);
    }
}

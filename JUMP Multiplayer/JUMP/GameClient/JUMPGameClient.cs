using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace JUMP
{
    public delegate void JUMPSnapshotReceivedDelegate(JUMPCommand_Snapshot snapshot);

    public class JUMPGameClient: MonoBehaviour
    {
        public event JUMPSnapshotReceivedDelegate OnSnapshotReceived;

        public void Awake()
        {
            PhotonNetwork.OnEventCall += OnPhotonEventCall;
        }

        public void OnDestroy()
        {
            PhotonNetwork.OnEventCall -= OnPhotonEventCall;
        }

        // receive snapshots from server - you can cast JUMPSnapshotData to your own custom type
        private void OnPhotonEventCall(byte eventCode, object content, int senderId)
        {
            if (eventCode == JUMPCommand_Snapshot.JUMPSnapshot_EventCode)
            {
                JUMPCommand_Snapshot snap = new JUMPCommand_Snapshot((object[])content);

                if (OnSnapshotReceived != null)
                {
                    OnSnapshotReceived(snap);
                }
            }
        }

        // The client needs to do the sending to the server, because with reliability turned on, it needs to send all the commands until the server acks them
        public void SendCommandToServer(JUMPCommand c)
        {
            RaiseEventOptions options = new RaiseEventOptions();
            options.Receivers = ExitGames.Client.Photon.ReceiverGroup.MasterClient;

            PhotonNetwork.RaiseEvent(c.CommandEventCode, c.CommandData, true, options);
        }

        // Send a connect command to the server with the photon client id
        public void ConnectToServer()
        {
            JUMPCommand_Connect c = new JUMPCommand_Connect(JUMPMultiplayer.PlayerID);
            SendCommandToServer(c);
        }

        public void Quit(JUMPMultiplayer.QuitReason reason)
        {
            // TODO: add send message to server when quitting
        }
    }
}

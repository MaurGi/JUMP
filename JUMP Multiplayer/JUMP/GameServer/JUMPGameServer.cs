using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JUMP
{
    public class JUMPGameServer : MonoBehaviour
    {
        // Server Engine
        private IJUMPGameServerEngine GameServerEngine;
        private List<JUMPPlayer> Players;

        private static TimeSpan SnapshotTimer = TimeSpan.Zero;
        private static TimeSpan SnapshotFrequency = TimeSpan.FromMilliseconds(1000 / JUMPOptions.SnapshotsPerSec);

        public void Awake()
        {
            PhotonNetwork.OnEventCall += OnPhotonEventCall;
        }

        void OnDestroy()
        {
            PhotonNetwork.OnEventCall -= OnPhotonEventCall;
        }

        // Update is called once per frame
        void Update()
        {
            if (PhotonNetwork.isMasterClient)
            {
                Tick(Time.deltaTime);
            }
        }

        // first you start the server passing the engine
        public void StartServer(IJUMPGameServerEngine gameServerEngine)
        {
            this.GameServerEngine = gameServerEngine;
            Players = new List<JUMPPlayer>();
        }

        public void Tick(double ElapsedSeconds)
        {
            GameServerEngine.Tick(ElapsedSeconds);
            // Is it time for a snapshot?
            SnapshotTimer += TimeSpan.FromSeconds(ElapsedSeconds);
            if (SnapshotTimer > SnapshotFrequency)
            {
                SnapshotTimer = TimeSpan.Zero;
                foreach (var player in Players)
                {
                    JUMPCommand_Snapshot snap = GameServerEngine.TakeSnapshot(player.PlayerID);

                    RaiseEventOptions options = new RaiseEventOptions();
                    options.TargetActors = new int[1] { snap.ForPlayerID };

                    PhotonNetwork.RaiseEvent(snap.CommandEventCode, snap.CommandData, true, options);
                }
            }
        }

        // Server Engine
        private void OnPhotonEventCall(byte eventCode, object content, int senderId)
        {
            // We are the server, hence, we are only processing events for the Master Client
            if (PhotonNetwork.isMasterClient)
            {
                if (eventCode == JUMPCommand_Connect.JUMPCommand_Connect_EventCode)
                {
                    JUMPCommand_Connect c = new JUMPCommand_Connect((object[])content);

                    // Set the player as connected
                    JUMPPlayer player = new JUMPPlayer();
                    player.PlayerID = c.PlayerID;
                    player.IsConnected = true;
                    Players.Add(player);

                    // When all the players are connected, start the game.
                    int connectedplayers = Players.Count(p => p.IsConnected);
                    if (connectedplayers == JUMPOptions.NumPlayers)
                    {
                        GameServerEngine.StartGame(Players);
                    }
                }
                else
                {
                    JUMPCommand c = GameServerEngine.CommandFromEvent(eventCode, content);
                    if (c != null)
                    {
                        GameServerEngine.ProcessCommand(c);
                    }
                }
            }
        }

        public void Quit(JUMPMultiplayer.QuitReason reason)
        {
            // TODO: add send message to server when quitting
        }
    }
}

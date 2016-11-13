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
        private List<IJUMPPlayer> Players;
        private List<IJUMPBot> Bots;

        private static TimeSpan SnapshotTimer = TimeSpan.Zero;
        private static TimeSpan SnapshotFrequency = TimeSpan.FromMilliseconds(1000 / JUMPOptions.SnapshotsPerSec);

        public bool IsOfflinePlayMode { get; private set; }

        JUMPGameServer()
        {
            IsOfflinePlayMode = false;
        }

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
            if (IsOfflinePlayMode || PhotonNetwork.isMasterClient)
            {
                Tick(Time.deltaTime);
            }
        }

        // first you start the server passing the engine
        public void StartServer(IJUMPGameServerEngine gameServerEngine, List<IJUMPBot> bots, bool isOfflinePlayMode)
        {
            GameServerEngine = gameServerEngine;
            Bots = bots;
            IsOfflinePlayMode = isOfflinePlayMode;
            Players = new List<IJUMPPlayer>();
        }

        public void Tick(double ElapsedSeconds)
        {
            GameServerEngine.Tick(ElapsedSeconds);
            Bots.ForEach(x => x.Tick(ElapsedSeconds));

            // Is it time for a snapshot?
            SnapshotTimer += TimeSpan.FromSeconds(ElapsedSeconds);
            if (SnapshotTimer > SnapshotFrequency)
            {
                SnapshotTimer = TimeSpan.Zero;
                foreach (var player in Players)
                {
                    // Bots don't get a snapshot, they have access to the whole engine with the full state of all players and bots
                    if (!(player is IJUMPBot))
                    {
                        JUMPCommand_Snapshot snapCommand = GameServerEngine.TakeSnapshot(player.PlayerID);

                        SendEventToClient(snapCommand, player.PlayerID);
                    }
                }
            }
        }

        private void SendEventToClient(JUMPCommand_Snapshot commandEvent, int? playerID = null)
        {
            if (IsOfflinePlayMode)
            {
                JUMPMultiplayer.GameClient.OnPhotonEventCall(commandEvent.CommandEventCode, commandEvent.CommandData, JUMPMultiplayer.PlayerID);
            }
            else
            {
                RaiseEventOptions options = null;
                if (playerID.HasValue)
                {
                    options = new RaiseEventOptions();
                    options.TargetActors = new int[1] { playerID.Value };
                }

                PhotonNetwork.RaiseEvent(commandEvent.CommandEventCode, commandEvent.CommandData, sendReliable: true, options: options);
            }
        }

        // Server Engine
        internal void OnPhotonEventCall(byte eventCode, object content, int senderId)
        {
            // We are the server, hence, we are only processing events for the Master Client
            if (IsOfflinePlayMode || PhotonNetwork.isMasterClient)
            {
                if (eventCode == JUMPCommand_Connect.JUMPCommand_Connect_EventCode)
                {
                    JUMPCommand_Connect c = new JUMPCommand_Connect((object[])content);

                    // Set the player as connected
                    JUMPPlayer player = new JUMPPlayer();
                    player.PlayerID = c.PlayerID;
                    player.IsConnected = true;
                    Players.Add(player);

                    int maxPlayerId = player.PlayerID;
                    // Add bots as players if offline
                    if (IsOfflinePlayMode)
                    {
                        Bots.ForEach(x =>
                        {
                            x.PlayerID = ++maxPlayerId;
                            x.IsConnected = true;
                            Players.Add(x);
                        });
                    }

                    int connectedplayers = Players.Count(p => p.IsConnected);

                    // When all the players are connected, start the game.
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

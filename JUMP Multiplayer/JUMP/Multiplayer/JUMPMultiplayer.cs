using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;

namespace JUMP
{
    [System.Serializable]
    public class JUMPSnapshotReceivedUnityEvent : UnityEvent<JUMPCommand_Snapshot> { }

    public class JUMPMultiplayer : Photon.PunBehaviour
    {
        public enum Stages
        {
            Connection,
            Master,
            MatchmakeLobby,
            GameRoom,
            Play
        }

        public enum QuitReason
        {
            WeCanceledMatchmake,
            WeCanceledGameRoom,
            WeLostConnection,
            WeQuit,
            TheyLostConnection
        }

        public Stages Stage = Stages.Connection;
        public static bool IsOffline { get { return PhotonNetwork.offlineMode; } }
        public static bool IsRoomFull {  get { return PhotonNetwork.inRoom ? (PhotonNetwork.room.playerCount == PhotonNetwork.room.maxPlayers) : false; } }
        public static int PlayerID { get { return (PhotonNetwork.player != null) ? (PhotonNetwork.player.ID) : -1; } }
        public DisconnectCause OfflineConnectReason { get; private set; }
        public QuitReason QuitGameReason { get; private set; }

        // Connection Stage
        public UnityEvent OnMasterConnect;

        // Master Stage 
        public static bool IsConnectedToMaster { get { return ((PhotonNetwork.connectionStateDetailed == PeerState.ConnectedToMaster) || (PhotonNetwork.connectionStateDetailed == PeerState.Authenticated)); } }
        public void Matchmake() { DoMatchmake();  }
        public UnityEvent OnMasterDisconnect;
        public UnityEvent OnMatchmakeLobbyConnect;

        // MatchmakeLobby Stage
        public static bool IsConnectedToMatchmakeLobby { get { return (PhotonNetwork.connectionStateDetailed == PeerState.JoinedLobby); } }
        public void CancelMatchmake() { DoCancelMatchmake(); }
        public UnityEvent OnMatchmakeLobbyDisconnect;
        public string GameServerEngineTypeName;
        public UnityEvent OnGameRoomConnect;

        // GameRoom Stage
        public static bool IsConnectedToGameRoom { get { return ((PhotonNetwork.connectionStateDetailed == PeerState.Joined)); } }
        public void CancelGameRoom() { DoCancelGameRoom();  }
        public UnityEvent OnGameRoomDisconnect;
        public UnityEvent OnPlayConnect;

        // Play Stage
        public static bool IsPlayingGame { get { return (IsConnectedToGameRoom && IsRoomFull); } }
        public void QuitPlay() { DoQuitPlay(QuitReason.WeQuit); }
        public JUMPSnapshotReceivedUnityEvent OnSnapshotReceived;
        public UnityEvent OnPlayDisconnected;

        // PRIVATE variables.
        private bool cancelingMatchmaking = false;
        private bool cancelingGameRoom = false;
        private bool quittingPlay = false;
        private bool attemptingToJoinOrCreateRoom = false;
        private bool applicationIsQuitting = false;

        #region UNITY EVENTS **************************************************
        /// <summary>
        /// Awake always called before any Start functions - let's use it to initialize
        /// the Photon settings at the Connection stage.
        /// </summary>
        void Awake() {
            LogInfo(() => FormatLogMessage("Unity.Awake"));

            // Reset local state changing variables
            cancelingMatchmaking = false;
            cancelingGameRoom = false;
            quittingPlay = false;
            attemptingToJoinOrCreateRoom = false;

            switch (Stage)
            {
                case Stages.Connection:
                    PhotonNetwork.offlineMode = false;
                    PhotonNetwork.PhotonServerSettings.EnableLobbyStatistics = true;
                    PhotonNetwork.networkingPeer.DisconnectTimeout = JUMPOptions.DisconnectTimeout;
                    break;
            }
        }

        // Use this for initialization
        void Start()
        {
            LogInfo(() => FormatLogMessage("Unity.Start"));
            switch (Stage)
            {
                // In the Connection Stage, we call connect and wait for the connection to complete or fail
                case Stages.Connection:
                    LogDebug(() => FormatLogMessage("Attempting to connect to Photon Server."));
                    PhotonNetwork.ConnectUsingSettings(JUMPOptions.GameVersion);
                    break;
                case Stages.Master:
                    // If we are not connected to Master and we are not online, we should go to the connection stage to connect
                    if (!IsConnectedToMaster && !IsOffline)
                    {
                        LogDebug(() => FormatLogMessage("We are not connected, need to either connect or figure out we are offline."));
                        RaiseOnMasterDisconnect();
                    }
                    break;
                case Stages.MatchmakeLobby:
                    if (IsConnectedToMatchmakeLobby && !IsOffline)
                    {
                        attemptingToJoinOrCreateRoom = true;
                        LogDebug(() => FormatLogMessage("In the matchmaking lobby, trying to join a random game"));
                        PhotonNetwork.JoinRandomRoom();
                    }
                    else
                    {
                        LogDebug(() => FormatLogMessage("We are not connected to the matchmake lobby, need to either connect or figure out we are offline."));
                        RaiseOnMatchmakeLobbyDisconnect();
                    }
                    break;
                case Stages.GameRoom:
                    if (IsConnectedToGameRoom)
                    {
                        LogDebug(() => FormatLogMessage("We are in a game room, let's start the game if the room is full."));
                        DoStartGameIfRoomFull();
                    }
                    else
                    {
                        LogDebug(() => FormatLogMessage("We are not connected to the game room, need to either connect or figure out we are offline."));
                        RaiseOnGameRoomDisconnect();
                    }
                    break;
                case Stages.Play:
                    if (!IsPlayingGame && !IsOffline)
                    {
                        LogDebug(() => FormatLogMessage("We are not in play, need to either connect or figure out we are offline."));
                        RaiseOnPlayDisconnected();
                    }
                    else
                    {
                        LogDebug(() => FormatLogMessage("We are in the game! Let's fire up the client."));
                        DoStartGameClient();
                    }
                    break;
                default:
                    break;
            }
        }

        // Update is called once per frame
        void Update()
        {
        }

        void OnApplicationQuit()
        {
            LogDebug(() => FormatLogMessage("Quitting => Disconnecting from Photon"));
            applicationIsQuitting = true;
            PhotonNetwork.Disconnect();
        }
        #endregion

        #region PHOTON EVENTS *************************************************
        // *** MASTER *** //
        /// <summary>
        /// Called after the connection to the master is established and authenticated but only when PhotonNetwork.autoJoinLobby is false.
        /// </summary>
        /// <remarks>
        /// If you set PhotonNetwork.autoJoinLobby to true, OnJoinedLobby() will be called instead of this.
        ///
        /// You can join rooms and create them even without being in a lobby. The default lobby is used in that case.
        /// The list of available rooms won't become available unless you join a lobby via PhotonNetwork.joinLobby.
        /// </remarks>
        public override void OnConnectedToMaster() {
            LogInfo(() => FormatLogMessage("Photon.OnConnectedToMaster"));
            if (Stage == Stages.Connection)
            {
                // Note: we are also called here if we set Offline=true.
                LogDebug(() => FormatLogMessage("We are connected to Photon, move on to the master screen. Offline Mode: {0}", PhotonNetwork.offlineMode));
                RaiseOnMasterConnect();
            }
            else if (Stage == Stages.GameRoom)
            {
                // Using the canceling game room.
                if (cancelingGameRoom)
                {
                    LogDebug(() => FormatLogMessage("Out of the room because game room was canceled, going back to main."));
                    cancelingGameRoom = false;
                    RaiseOnGameRoomDisconnect();
                }
            }
            else if (Stage == Stages.Play)
            {
                // Using the quitting play.
                if (quittingPlay)
                {
                    LogDebug(() => FormatLogMessage("Out of the room because play was quitted, going back to main."));
                    quittingPlay = false;
                    RaiseOnPlayDisconnected();
                }
            }
        }

        /// <summary>
        /// Called if a connect call to the Photon server failed before the connection was established, followed by a call to OnDisconnectedFromPhoton().
        /// </summary>
        /// <remarks>
        /// This is called when no connection could be established at all.
        /// It differs from OnConnectionFail, which is called when an existing connection fails.
        /// </remarks>
        public override void OnFailedToConnectToPhoton(DisconnectCause cause)
        {
            LogInfo(() => FormatLogMessage("Photon.OnFailedToConnectToPhoton"));
            // This should only happen in the connection stage - after that we are connected.
            if (Stage == Stages.Connection)
            {
                LogError(() => FormatLogMessage("Failed to connected to Master Server: {0}", cause.ToString()));
                OfflineConnectReason = cause;
                // OnDisconnectedFromPhoton will be called by Photon now.
            }
        }

        /// <summary>
        /// Called after disconnecting from the Photon server.
        /// </summary>
        /// <remarks>
        /// In some cases, other callbacks are called before OnDisconnectedFromPhoton is called.
        /// Examples: OnConnectionFail() and OnFailedToConnectToPhoton().
        /// </remarks>
        public override void OnDisconnectedFromPhoton()
        {
            // we get some calls sometimes from Photon when we are in Edit mode
            if (!Application.isPlaying || applicationIsQuitting)
            {
                if (applicationIsQuitting)
                {
                    LogDebug(() => FormatLogMessage("Disconnected from Photon while quitting"));
                }
                else if (!Application.isPlaying)
                {
                    LogDebug(() => FormatLogMessage("Disconnected from Photon while not playing"));
                }

                return;
            }

            LogInfo(() => FormatLogMessage("Photon.OnDisconnectedFromPhoton"));
            switch (Stage)
            {
                case Stages.Connection:
                case Stages.Master:
                case Stages.MatchmakeLobby:
                case Stages.GameRoom:
                case Stages.Play:
                    LogDebug(() => FormatLogMessage("Going offline, there is no connection to Photon."));
                    // Setting this will trigger the OnConnectedToMaster, but offlinemode will be true;
                    PhotonNetwork.offlineMode = true;
                    if (Stage == Stages.Master) RaiseOnMasterDisconnect();
                    if (Stage == Stages.MatchmakeLobby)
                    {
                        cancelingMatchmaking = false;
                        attemptingToJoinOrCreateRoom = false;
                        RaiseOnMatchmakeLobbyDisconnect();
                    }
                    if (Stage == Stages.GameRoom)
                    {
                        cancelingGameRoom = false;
                        RaiseOnGameRoomDisconnect();
                    }
                    if (Stage == Stages.Play)
                    {
                        if (!quittingPlay)
                        {
                            // we were not trying to quit, we lost connection
                            DoQuitPlay(QuitReason.WeLostConnection);
                        }
                        quittingPlay = false;
                        RaiseOnPlayDisconnected();
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Called when something causes the connection to fail (after it was established), followed by a call to OnDisconnectedFromPhoton().
        /// </summary>
        /// <remarks>
        /// If the server could not be reached in the first place, OnFailedToConnectToPhoton is called instead.
        /// The reason for the error is provided as DisconnectCause.
        /// </remarks>
        public override void OnConnectionFail(DisconnectCause cause)
        {
            LogInfo(() => FormatLogMessage("Photon.OnConnectionFail"));
            // This should only happen after the connection stage when we are connected.
            if (Stage != Stages.Connection)
            {
                LogError(() => FormatLogMessage("The connection to photon failed : {0}", cause.ToString()));
                OfflineConnectReason = cause;
                // OnDisconnectedFromPhoton will be called by Photon now.
            }
        }

        // *** LOBBY *** //
        /// <summary>
        /// Called on entering a lobby on the Master Server. The actual room-list updates will call OnReceivedRoomListUpdate().
        /// </summary>
        /// <remarks>
        /// Note: When PhotonNetwork.autoJoinLobby is false, OnConnectedToMaster() will be called and the room list won't become available.
        ///
        /// While in the lobby, the roomlist is automatically updated in fixed intervals (which you can't modify).
        /// The room list gets available when OnReceivedRoomListUpdate() gets called after OnJoinedLobby().
        /// </remarks>
        public override void OnJoinedLobby()
        {
            LogInfo(() => FormatLogMessage("Photon.OnJoinedLobby"));
            if (Stage == Stages.Master)
            {
                LogDebug(() => FormatLogMessage("We joined the lobby, go to the MatchmakingLobby stage."));
                RaiseOnMatchmakeLobbyConnect();
            }
        }

        /// <summary>
        /// Called after leaving a lobby.
        /// </summary>
        /// <remarks>
        /// When you leave a lobby, [CreateRoom](@ref PhotonNetwork.CreateRoom) and [JoinRandomRoom](@ref PhotonNetwork.JoinRandomRoom)
        /// automatically refer to the default lobby.
        /// </remarks>
        public override void OnLeftLobby() {
            LogInfo(() => FormatLogMessage("Photon.OnLeftLobby"));
            // Using the canceling matchmaking flag.
            if (cancelingMatchmaking)
            {
                LogDebug(() => FormatLogMessage("Out of the lobby becayse matchmake was canceled, going back to main."));
                cancelingMatchmaking = false;
                RaiseOnMatchmakeLobbyDisconnect();
            }
        }

        // *** ROOM *** //
        /// <summary>
        /// Called when a CreateRoom() call failed. The parameter provides ErrorCode and message (as array).
        /// </summary>
        /// <remarks>
        /// Most likely because the room name is already in use (some other client was faster than you).
        /// PUN logs some info if the PhotonNetwork.logLevel is >= PhotonLogLevel.Informational.
        /// </remarks>
        /// <param name="codeAndMsg">codeAndMsg[0] is a short ErrorCode and codeAndMsg[1] is a string debug msg.</param>
        public override void OnPhotonCreateRoomFailed(object[] codeAndMsg)
        {
            LogInfo(() => FormatLogMessage("Photon.OnPhotonCreateRoomFailed: {0}, {1}.", codeAndMsg[0], codeAndMsg[1]));
            if (Stage == Stages.MatchmakeLobby)
            {
                attemptingToJoinOrCreateRoom = false;
                LogError(() => FormatLogMessage("We are in trouble, can't join and can't create a room have to cancel game room!"));
                DoCancelMatchmake();
            }
        }

        /// <summary>
        /// Called when this client created a room and entered it. OnJoinedRoom() will be called as well.
        /// </summary>
        /// <remarks>
        /// This callback is only called on the client which created a room (see PhotonNetwork.CreateRoom).
        ///
        /// As any client might close (or drop connection) anytime, there is a chance that the
        /// creator of a room does not execute OnCreatedRoom.
        ///
        /// If you need specific room properties or a "start signal", it is safer to implement
        /// OnMasterClientSwitched() and to make the new MasterClient check the room's state.
        /// </remarks>
        public override void OnCreatedRoom()
        {
            LogInfo(() => FormatLogMessage("Photon.OnCreatedRoom"));
            // Will also call OnJoinedRoom now, which will move us to the game scene
        }

        /// <summary>
        /// Called when entering a room (by creating or joining it). Called on all clients (including the Master Client).
        /// </summary>
        /// <remarks>
        /// This method is commonly used to instantiate player characters.
        /// If a match has to be started "actively", you can call an [PunRPC](@ref PhotonView.RPC) triggered by a user's button-press or a timer.
        ///
        /// When this is called, you can usually already access the existing players in the room via PhotonNetwork.playerList.
        /// Also, all custom properties should be already available as Room.customProperties. Check Room.playerCount to find out if
        /// enough players are in the room to start playing.
        /// </remarks>
        public override void OnJoinedRoom()
        {
            LogInfo(() => FormatLogMessage("Photon.OnJoinedRoom"));
            if (Stage == Stages.MatchmakeLobby)
            {
                attemptingToJoinOrCreateRoom = false;

                // Start the game server if we are the master
                if (PhotonNetwork.isMasterClient)
                {
                    LogDebug(() => FormatLogMessage("We created the room as we are the master - go ahead and start the game server."));
                    DoStartGameServer();
                }

                LogDebug(() => FormatLogMessage("We joined the room, go to the GameRoom stage."));
                RaiseOnGameRoomConnect();
            }
        }

        /// <summary>
        /// Called when the local user/client left a room.
        /// </summary>
        /// <remarks>
        /// When leaving a room, PUN brings you back to the Master Server.
        /// Before you can use lobbies and join or create rooms, OnJoinedLobby() or OnConnectedToMaster() will get called again.
        /// </remarks>
        public override void OnLeftRoom()
        {
            LogInfo(() => FormatLogMessage("Photon.OnLeftRoom"));
            // for canceling the game room, let's wait for the client to join back the master server
        }

        /// <summary>
        /// Called when a JoinRoom() call failed. The parameter provides ErrorCode and message (as array).
        /// </summary>
        /// <remarks>
        /// Most likely error is that the room does not exist or the room is full (some other client was faster than you).
        /// PUN logs some info if the PhotonNetwork.logLevel is >= PhotonLogLevel.Informational.
        /// </remarks>
        /// <param name="codeAndMsg">codeAndMsg[0] is short ErrorCode. codeAndMsg[1] is string debug msg.</param>
        public override void OnPhotonJoinRoomFailed(object[] codeAndMsg) { }

        /// <summary>
        /// Called when a JoinRandom() call failed. The parameter provides ErrorCode and message.
        /// </summary>
        /// <remarks>
        /// Most likely all rooms are full or no rooms are available. <br/>
        /// When using multiple lobbies (via JoinLobby or TypedLobby), another lobby might have more/fitting rooms.<br/>
        /// PUN logs some info if the PhotonNetwork.logLevel is >= PhotonLogLevel.Informational.
        /// </remarks>
        /// <param name="codeAndMsg">codeAndMsg[0] is short ErrorCode. codeAndMsg[1] is string debug msg.</param>
        public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
        {
            LogInfo(() => FormatLogMessage("Photon.OnPhotonRandomJoinFailed: {0}, {1}.", codeAndMsg[0], codeAndMsg[1]));
            if (Stage == Stages.MatchmakeLobby)
            {
                LogDebug(() => FormatLogMessage("We could not join a room, let's create one and wait for players."));
                attemptingToJoinOrCreateRoom = true;
                RoomOptions opt = new RoomOptions();
                opt.maxPlayers = JUMPOptions.NumPlayers;
                opt.isOpen = true;
                opt.isVisible = true;
                PhotonNetwork.CreateRoom(null, opt, null);
            }
        }

        // *** PLAYER CONNECTED *** //
        /// <summary>
        /// Called when a remote player entered the room. This PhotonPlayer is already added to the playerlist at this time.
        /// </summary>
        /// <remarks>
        /// If your game starts with a certain number of players, this callback can be useful to check the
        /// Room.playerCount and find out if you can start.
        /// </remarks>
        public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
        {
            LogInfo(() => FormatLogMessage("Photon.OnPhotonPlayerConnected: {0}.", newPlayer.ID));
            if (Stage == Stages.GameRoom)
            {
                LogDebug(() => FormatLogMessage("A new player joined, we might need to start the game."));
                DoStartGameIfRoomFull();
            }
        }

        /// <summary>
        /// Called when a remote player left the room. This PhotonPlayer is already removed from the playerlist at this time.
        /// </summary>
        /// <remarks>
        /// When your client calls PhotonNetwork.leaveRoom, PUN will call this method on the remaining clients.
        /// When a remote client drops connection or gets closed, this callback gets executed. after a timeout
        /// of several seconds.
        /// </remarks>
        public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
        {
            LogInfo(() => FormatLogMessage("Photon.OnPhotonPlayerDisconnected: {0}.", otherPlayer.ID));
            if (Stage == Stages.Play)
            {
                LogDebug(() => FormatLogMessage("A player disconnected, we have to bail, we don't support host migration."));
                DoQuitPlay(QuitReason.TheyLostConnection);
            }
        }
        #endregion

        #region NUT USED PHOTON EVENTS 
        /// <summary>
        /// Called for any update of the room-listing while in a lobby (PhotonNetwork.insideLobby) on the Master Server.
        /// </summary>
        /// <remarks>
        /// PUN provides the list of rooms by PhotonNetwork.GetRoomList().<br/>
        /// Each item is a RoomInfo which might include custom properties (provided you defined those as lobby-listed when creating a room).
        ///
        /// Not all types of lobbies provide a listing of rooms to the client. Some are silent and specialized for server-side matchmaking.
        /// </remarks>
        //public override void OnReceivedRoomListUpdate() { }

        // JUMP: We will handle the OnConnectedToMaster event
        /// <summary>
        /// Called when the initial connection got established but before you can use the server. OnJoinedLobby() or OnConnectedToMaster() are called when PUN is ready.
        /// </summary>
        /// <remarks>
        /// This callback is only useful to detect if the server can be reached at all (technically).
        /// Most often, it's enough to implement OnFailedToConnectToPhoton() and OnDisconnectedFromPhoton().
        ///
        /// <i>OnJoinedLobby() or OnConnectedToMaster() are called when PUN is ready.</i>
        ///
        /// When this is called, the low level connection is established and PUN will send your AppId, the user, etc in the background.
        /// This is not called for transitions from the masterserver to game servers.
        /// </remarks>
        //public override void OnConnectedToPhoton() {}

        // JUMP: Not handled in this version, we only support 2 player games and when the master client leaves, the game is over.
        /// <summary>
        /// Called after switching to a new MasterClient when the current one leaves.
        /// </summary>
        /// <remarks>
        /// This is not called when this client enters a room.
        /// The former MasterClient is still in the player list when this method get called.
        /// </remarks>
        //public override void OnMasterClientSwitched(PhotonPlayer newMasterClient) {}

        /// <summary>
        /// Called on all scripts on a GameObject (and children) that have been Instantiated using PhotonNetwork.Instantiate.
        /// </summary>
        /// <remarks>
        /// PhotonMessageInfo parameter provides info about who created the object and when (based off PhotonNetworking.time).
        /// </remarks>
        //public override void OnPhotonInstantiate(PhotonMessageInfo info) { }

        /// <summary>
        /// Because the concurrent user limit was (temporarily) reached, this client is rejected by the server and disconnecting.
        /// </summary>
        /// <remarks>
        /// When this happens, the user might try again later. You can't create or join rooms in OnPhotonMaxCcuReached(), cause the client will be disconnecting.
        /// You can raise the CCU limits with a new license (when you host yourself) or extended subscription (when using the Photon Cloud).
        /// The Photon Cloud will mail you when the CCU limit was reached. This is also visible in the Dashboard (webpage).
        /// </remarks>
        //public override void OnPhotonMaxCccuReached() { }

        /// <summary>
        /// Called when a room's custom properties changed. The propertiesThatChanged contains all that was set via Room.SetCustomProperties.
        /// </summary>
        /// <remarks>
        /// Since v1.25 this method has one parameter: Hashtable propertiesThatChanged.<br/>
        /// Changing properties must be done by Room.SetCustomProperties, which causes this callback locally, too.
        /// </remarks>
        /// <param name="propertiesThatChanged"></param>
        //public override void OnPhotonCustomRoomPropertiesChanged(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }

        /// <summary>
        /// Called when custom player-properties are changed. Player and the changed properties are passed as object[].
        /// </summary>
        /// <remarks>
        /// Since v1.25 this method has one parameter: object[] playerAndUpdatedProps, which contains two entries.<br/>
        /// [0] is the affected PhotonPlayer.<br/>
        /// [1] is the Hashtable of properties that changed.<br/>
        ///
        /// We are using a object[] due to limitations of Unity's GameObject.SendMessage (which has only one optional parameter).
        ///
        /// Changing properties must be done by PhotonPlayer.SetCustomProperties, which causes this callback locally, too.
        ///
        /// Example:<pre>
        /// void OnPhotonPlayerPropertiesChanged(object[] playerAndUpdatedProps) {
        ///     PhotonPlayer player = playerAndUpdatedProps[0] as PhotonPlayer;
        ///     Hashtable props = playerAndUpdatedProps[1] as Hashtable;
        ///     //...
        /// }</pre>
        /// </remarks>
        /// <param name="playerAndUpdatedProps">Contains PhotonPlayer and the properties that changed See remarks.</param>
        //public override void OnPhotonPlayerPropertiesChanged(object[] playerAndUpdatedProps) { }

        /// <summary>
        /// Called when the server sent the response to a FindFriends request and updated PhotonNetwork.Friends.
        /// </summary>
        /// <remarks>
        /// The friends list is available as PhotonNetwork.Friends, listing name, online state and
        /// the room a user is in (if any).
        /// </remarks>
        //public override void OnUpdatedFriendList() { }

        /// <summary>
        /// Called when the custom authentication failed. Followed by disconnect!
        /// </summary>
        /// <remarks>
        /// Custom Authentication can fail due to user-input, bad tokens/secrets.
        /// If authentication is successful, this method is not called. Implement OnJoinedLobby() or OnConnectedToMaster() (as usual).
        ///
        /// During development of a game, it might also fail due to wrong configuration on the server side.
        /// In those cases, logging the debugMessage is very important.
        ///
        /// Unless you setup a custom authentication service for your app (in the [Dashboard](https://www.exitgames.com/dashboard)),
        /// this won't be called!
        /// </remarks>
        /// <param name="debugMessage">Contains a debug message why authentication failed. This has to be fixed during development time.</param>
        //public override void OnCustomAuthenticationFailed(string debugMessage) { }

        /// <summary>
        /// Called when your Custom Authentication service responds with additional data.
        /// </summary>
        /// <remarks>
        /// Custom Authentication services can include some custom data in their response.
        /// When present, that data is made available in this callback as Dictionary.
        /// While the keys of your data have to be strings, the values can be either string or a number (in Json).
        /// You need to make extra sure, that the value type is the one you expect. Numbers become (currently) int64.
        ///
        /// Example: void OnCustomAuthenticationResponse(Dictionary&lt;string, object&gt; data) { ... }
        /// </remarks>
        /// <see cref="https://doc.photonengine.com/en/realtime/current/reference/custom-authentication"/>
        //public override void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }

        /// <summary>
        /// Called by PUN when the response to a WebRPC is available. See PhotonNetwork.WebRPC.
        /// </summary>
        /// <remarks>
        /// Important: The response.ReturnCode is 0 if Photon was able to reach your web-service.
        /// The content of the response is what your web-service sent. You can create a WebResponse instance from it.
        /// Example: WebRpcResponse webResponse = new WebRpcResponse(operationResponse);
        ///
        /// Please note: Class OperationResponse is in a namespace which needs to be "used":
        /// using ExitGames.Client.Photon;  // includes OperationResponse (and other classes)
        ///
        /// The OperationResponse.ReturnCode by Photon is:<pre>
        ///  0 for "OK"
        /// -3 for "Web-Service not configured" (see Dashboard / WebHooks)
        /// -5 for "Web-Service does now have RPC path/name" (at least for Azure)</pre>
        /// </remarks>
        //public override void OnWebRpcResponse(OperationResponse response) { }

        /// <summary>
        /// Called when another player requests ownership of a PhotonView from you (the current owner).
        /// </summary>
        /// <remarks>
        /// The parameter viewAndPlayer contains:
        ///
        /// PhotonView view = viewAndPlayer[0] as PhotonView;
        ///
        /// PhotonPlayer requestingPlayer = viewAndPlayer[1] as PhotonPlayer;
        /// </remarks>
        /// <param name="viewAndPlayer">The PhotonView is viewAndPlayer[0] and the requesting player is viewAndPlayer[1].</param>
        //public override void OnOwnershipRequest(object[] viewAndPlayer) { }

        /// <summary>
        /// Called when the Master Server sent an update for the Lobby Statistics, updating PhotonNetwork.LobbyStatistics.
        /// </summary>
        /// <remarks>
        /// This callback has two preconditions:
        /// EnableLobbyStatistics must be set to true, before this client connects.
        /// And the client has to be connected to the Master Server, which is providing the info about lobbies.
        /// </remarks>
        //public override void OnLobbyStatisticsUpdate() { }
        #endregion

        #region JUMP Actions **************************************************
        private void DoMatchmake()
        {
            LogInfo(() => FormatLogMessage("JUMP.Matchmake"));
            LogDebug(() => FormatLogMessage("Trying to join the lobby so we can matchmake."));
            PhotonNetwork.JoinLobby();
        }

        private void DoCancelMatchmake()
        {
            LogInfo(() => FormatLogMessage("JUMP.DoCancelMatchmake"));
            if (PhotonNetwork.insideLobby)
            {
                if (attemptingToJoinOrCreateRoom)
                {
                    LogDebug(() => FormatLogMessage("Trying to cancel matchmake, but it's too late, we are already joining a room."));
                }
                else
                {
                    LogDebug(() => FormatLogMessage("Matchmake canceled, leaving the lobby."));

                    cancelingMatchmaking = true;
                    PhotonNetwork.LeaveLobby();
                }
            }
        }

        private void DoCancelGameRoom()
        {
            LogInfo(() => FormatLogMessage("JUMP.DoCancelGameRoom"));
            if (PhotonNetwork.inRoom)
            {
                LogDebug(() => FormatLogMessage("Game Room canceled, leaving the room."));

                // shut down the server
                if (PhotonNetwork.isMasterClient)
                {
                    Singleton<JUMPGameServer>.Instance.Quit(QuitReason.WeCanceledGameRoom);
                    Singleton<JUMPGameServer>.DestroyInstance();
                }

                cancelingGameRoom = true;
                PhotonNetwork.LeaveRoom();
            }
        }

        private void DoStartGameIfRoomFull()
        {
            LogInfo(() => FormatLogMessage("JUMP.DoStartGameIfRoomFull"));

            // when two players join the room, then we create the client and the server
            if (IsRoomFull)
            {
                LogDebug(() => FormatLogMessage("The room is full, starting the Server and moving to the Play scene"));

                // Move on to the play scene
                RaiseOnPlayConnect();
            }
            else
            {
                LogDebug(() => FormatLogMessage("The room is not full yet"));
            }
        }
          
        private void DoStartGameServer()
        {
            LogInfo(() => FormatLogMessage("JUMP.DoStartGameServer"));

            if (!PhotonNetwork.isMasterClient)
            {
                LogError(() => FormatLogMessage("Trying to start a game server not on the master client"));
                return;
            }

            if (string.IsNullOrEmpty(GameServerEngineTypeName))
            {
                LogError(() => FormatLogMessage("Trying to start a game server with a null GameServerEngine object"));
            }
            else
            {
                LogInfo(() => FormatLogMessage("Starting a game server with Server of class: {0}", GameServerEngineTypeName));
                Type T = Type.GetType(GameServerEngineTypeName);
                if (T == null)
                {
                    LogError(() => FormatLogMessage("To start a game server you have to set the GameServerEngineTypeName parameter to a valid type name"));
                    return;
                }

                var instance = Activator.CreateInstance(T);

                if (instance is IJUMPGameServerEngine)
                {
                    if (PhotonNetwork.isMasterClient)
                    {
                        Singleton<JUMPGameServer>.Instance.StartServer(instance as IJUMPGameServerEngine);
                    }
                    else
                    {
                        LogError(() => FormatLogMessage("Trying to start a server outside of the Master Client"));
                    }
                }
                else
                {
                    LogError(() => FormatLogMessage("To start a game server it has to implement the IJUMPGameServerEngine interface"));
                }
            }
        }

        private void DoStartGameClient()
        {
            LogInfo(() => FormatLogMessage("JUMP.DoStartGameClient"));

            LogInfo(() => FormatLogMessage("Starting a game client"));
            Singleton<JUMPGameClient>.Instance.OnSnapshotReceived += RaiseOnSnapshotReceived;
            Singleton<JUMPGameClient>.Instance.ConnectToServer();
        }

        private void DoQuitPlay(QuitReason reason)
        {
            LogInfo(() => FormatLogMessage("JUMP.DoQuitPlay, reason: {0}", reason));

            QuitGameReason = reason;

            // quit client and destroy it
            Singleton<JUMPGameClient>.Instance.Quit(reason);
            Singleton<JUMPGameClient>.DestroyInstance();

            if (PhotonNetwork.isMasterClient)
            {
                // quit server and destroy it
                Singleton<JUMPGameServer>.Instance.Quit(reason);
                Singleton<JUMPGameServer>.DestroyInstance();
            }

            // leave the room if we are still in there
            if (PhotonNetwork.inRoom)
            {
                LogDebug(() => FormatLogMessage("Quit play invoked, leaving the room."));
                quittingPlay = true;
                PhotonNetwork.LeaveRoom();
            }
        }
        #endregion

        #region JUMP Events ***************************************************
        private void RaiseOnMasterConnect()
        {
            LogInfo(() => FormatLogMessage("JUMP.RaiseOnMasterConnect"));
            OnMasterConnect.Invoke();
        }

        private void RaiseOnMasterDisconnect()
        {
            LogInfo(() => FormatLogMessage("JUMP.RaiseOnMasterDisconnect"));
            OnMasterDisconnect.Invoke();
        }

        private void RaiseOnMatchmakeLobbyConnect()
        {
            LogInfo(() => FormatLogMessage("JUMP.RaiseOnMatchmakeLobbyConnect"));
            OnMatchmakeLobbyConnect.Invoke();
        }

        private void RaiseOnMatchmakeLobbyDisconnect()
        {
            LogInfo(() => FormatLogMessage("JUMP.RaiseOnMatchmakeLobbyDisconnect"));
            OnMatchmakeLobbyDisconnect.Invoke();
        }

        private void RaiseOnGameRoomConnect()
        {
            LogInfo(() => FormatLogMessage("JUMP.RaiseOnGameRoomConnect"));
            OnGameRoomConnect.Invoke();
        }

        private void RaiseOnGameRoomDisconnect()
        {
            LogInfo(() => FormatLogMessage("JUMP.RaiseOnGameRoomDisconnect"));
            OnGameRoomDisconnect.Invoke();
        }

        private void RaiseOnPlayConnect()
        {
            LogInfo(() => FormatLogMessage("JUMP.RaiseOnPlayConnect"));
            OnPlayConnect.Invoke();
        }

        private void RaiseOnPlayDisconnected()
        {
            LogInfo(() => FormatLogMessage("JUMP.RaiseOnPlayDisconnected"));
            OnPlayDisconnected.Invoke();
        }

        private void RaiseOnSnapshotReceived(JUMPCommand_Snapshot snapshot)
        {
            LogVerbose(() => FormatLogMessage("JUMP.RaiseOnSnapshotReceived"));
            OnSnapshotReceived.Invoke(snapshot);
        }
        #endregion

        #region Logging *****************************************************
        public enum LogLevels
        {
            Verbose = 3,
            Debug = 2,
            Info = 1,
            Error = 0,
        }

        #if DEBUG
        public LogLevels LogLevel = LogLevels.Debug;
        #else
        public LogLevels LogLevel = LogLevels.Error;
        #endif
        private void Log(LogLevels level, Func<string> message)
        {
            if ((int)LogLevel >= (int)level)
            {
                switch (level)
                {
                    case LogLevels.Error:
                        Debug.LogError(message());
                        break;
                    default:
                        Debug.Log(message());
                        break;
                }
            }
        }

        private void LogVerbose(Func<string> message)
        {
            Log(LogLevels.Verbose, message);
        }

        private void LogDebug(Func<string> message)
        {
            Log(LogLevels.Debug, message);
        }

        private void LogInfo(Func<string> message)
        {
            Log(LogLevels.Info, message);
        }

        private void LogError(Func<string> message)
        {
            Log(LogLevels.Error, message);
        }

        private string FormatLogMessage(string format, params object[] args)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendFormat("JUMP [{0}]({1}/{2}) Stage: {3} - Message: {4}", DateTime.Now.ToString("O"), this.gameObject.scene.name, this.gameObject.name, Stage, string.Format(format, args));
            return sb.ToString();
        }
        #endregion
    }
}

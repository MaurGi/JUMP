# Object Model
JUMP Uses the following classes:
* [`JUMPMultiplayer`](#jumpmultiplayer)
* [`JUMPGameClient`](#jumpgameclient)
* [`JUMPGameServer`](#jumpgameserver)
* [`IJUMPGameServerEngine`](#ijumpgameserverengine)
* [`JUMPCommand`](#jumpcommand)
* [`JUMPCommand_Snapshot`](#jumpcommand_snapshot)
* [`JUMPPlayer`](#jumpplayer)
* [`JUMPOptions`](#jumpoptions)
* ['IJUMPBot'](#ijumpbot)

#### JUMPMultiplayer
`JUMPMultiplayer` is the main class in **JUMP**, handling the connection to the server, the matchmaking, setting up the game room and managing the game client and server. 
`JUMPMultiplayer` inherits from [`Photon.PunBehaviour`](http://doc-api.exitgames.com/en/pun/current/class_photon_1_1_pun_behaviour.html), which in turn extends `UnityEngine.MonoBehaviour` to interact both with Photon Networking and Unity Scenes.

To use `JUMPMultiplayer`, you place it on a Unity Scene, set the `Stage` property and handle the UnityEvents that are raised by loading other scenes and setting a different Stage or issuing commands. See [DiceRoller Sample](#diceroller-sample) for an example.

To facilitate development, **JUMP** provides a series of **JUMPMultiplayer Prefabs** see details [here](#jumpmultplayer-prefabs) 

##### `JUMPMultiplayer.Stage`
`JUMPMultiplayer` works in stages, one for each of the [Steamlined UI flow](#streamlined-ui-flow)

```c#
public enum Stages
{
    Connection,
    Master,
    GameRoom,
    Play,
	OfflinePlay
}
```

At high level the `Stages` state diagram is the following:

![](https://raw.githubusercontent.com/MaurGi/JUMP/master/Doc/StateDiagram.png)

In more detail, based on the current stage, `JUMPMultiplayer` provides the following events and operations:
##### `Stages.Connection`
In the `Connection` stage, JUMP will try to connect to Photon Server, using the [Photon connection settings](https://doc-api.photonengine.com/en/pun/current/class_photon_network.html#a0fdb79bcce45801ec81fbe56ffb939ec):

When the connection is established, the `OnMasterConnnect` event is raised:
```c#
public UnityEvent OnMasterConnect;
```
You respond to the event by loading a different Unity Scene with a `JUMPMultiplayer` object set in to the `Master` stage.
Note that if the connection fails, then the `OnMasterConnect` event is raised, but the `JUMPMultiplayer.IsOffline` property is set to true.
##### `Stages.Master`
The `Master` stage is the main screen one, the user is connected to Photon, but not yet into a game room.
While connected to the Photon Master server (but not in a Room), the `JUMPMultiplayer.IsConnectedToMaster` property will be set to `true`.

To start the matchmaking process, you call the `Matchmake` operation:
```c#
public void Matchmake()
```
This will make JUMP try to matchmake and connect to a [Photon Game Room](https://doc.photonengine.com/en/realtime/current/reference/matchmaking-and-lobby) using Randon matchmaking.

When a game room is found then the `OnGameRoomConnect` event is fired:
```c#''
public UnityEvent OnGameRoomConnect;
```
If no room is not found, then one is created and the user is joined to it waiting for other players. The same `OnGameRoomConnect` event is fired.
You want to handle this even by loading a scene that tells the users they are waiting for the other player to connect.

When the client connects to the Game Room, an instance of the [`JUMPGameServer`](#jumpgameserver) is created - this will invoke your custom Server Engine. Just implement the [`IJUMPGameServerEngine`](#ijumpgameserverengine) interface in your class and provide the name of the class (including the namespace) to the `GameServerEngineTypeName` property:
```c#
public string GameServerEngineTypeName;
```

If the connection fails or we lose connection to the PhotonServer, then the `OnMasterDisconnect` event is fired. You want to handle this event by going back to the 'Connection' scene to try and reconnect once - if reconnection fails, you will be navigate again to the main scree with the `IsOffline` property set to true - in which case, you want to tell your users that they are offline. 
```c#
public UnityEvent OnMasterDisconnect;
```

For offline play with bots, use:
```c#
public void OfflinePlay()
```

This will set up offline server and client as well as creating Bots (implementing the IJUMPBots interface) and then trigger the `OnOfflinePlayConnect' event.


##### `Stages.GameRoom`
In this stage, the player is connected to a [Photon Game Room](https://doc.photonengine.com/en/realtime/current/reference/matchmaking-and-lobby) and waiting for the room to be full with two players.
While connected to the Game Room, the `JUMPMultiplayer.IsConnectedToGameRoom` property will be set to `true`.

You can cancel the action and get out of the Game Room, by calling the `CancelGameRoom` operation:
```c#
public void CancelGameRoom()
```
This will cancel the Game Room request and raise the `OnGameRoomDisconnect` event:
```c#
public UnityEvent OnGameRoomDisconnect;
```
The `OnGameRoomDisconnect` event is also triggered if you lose connection to Photon. You want to handle this event by going back to the Main screen ([`Stages.Master`](#stagesmaster)).

When the second player connects, then the `OnPlayConnect` event is fired:
```c#
public UnityEvent OnPlayConnect;
```
The `OnPlayConnect` event is also fired if the room is already present and you are joining as the second player; in this case you will not have the time to cancel the game room request.
You want to handle the `OnPlayConnect` event by going to the Game Play scene.
##### `Stages.Play`
This is the stage where the play happens, both players are joined to a Photon Game Room and exchanging commands and snapshots with the server to play the game. While in the Play stage, the `JUMPMultiplayer.IsPlayingGame` variable is set to true; note that this is a combination of both the `IsConnectedToGameRoom` and `IsRoomFull` properties.

You can cancel the game and get out of the room by calling `QuitPlay`:
```c#
public void QuitPlay()
```
This will exit the game room and trigger the `OnPlayDisconnected` event:
```c#
public UnityEvent OnPlayDisconnected;
```
The `OnPlayDisconnected` event is also fired if the other player leaves the room or if you lose connection to Photon.
You want to handle the `OnPlayDisconnected` event by telling the user the reason for the disconnection (using the `QuitGameReason` property) and then moving back to the Main screen ([`Stages.Master`](#stagesmaster)).

When `JUMPMulyiplayer` enters in the Play stage, then the [`JUMPGameClient`](#jumpgameclient) is initialized. At this point the client connects to the [`JUMPGameServer`](#jumpgameserver) that in turn starts sending **Snapshots** to the client.
The `JUMPMultiplayer` will raise an `OnSnapshotReceived` event every time an snapshot is sent from the server to the client.
For more information on how to handle the Snapshots, see the [`JUMPGameServer`](#jumpgameserver) section.
```c#
public JUMPSnapshotReceivedUnityEvent OnSnapshotReceived;
```

##### `Stages.OfflinePlay`
In this stage, the `IsOfflinePlay` property is set to true.
You will receive regular events like with online play `OnSnapshotReceived` and you can send command as well `GameClient.SendCommand(Command)`.
You can quit calling `QuitPlay`, same as in the online play scenario.

##### `JUMPMultiplayer.PlayerID`
Provides the ID of the Photon player object (or -1 if you are not connected to Photon).

#### JUMPMultplayer Prefabs
The _/JUMP/Multiplayer_ folder contains five prefabs, one for each of the [Stages](#jumpmultiplayerstage).
The prefabs are:
* JUMPMultiplayerConnection
* JUMPMultiplayerMaster
* JUMPMultiplayerGameRoom
* JUMPMultiplayerPlay

The prefabs are simply a Game Object with a `JUMPMultiplayer` component set to the relative `Stage`. The idea is to place these in each of the five scenes that will compose the [UI Flow](#streamlined-ui-flow)

#### JUMPGameClient
`JUMPGameClient` uses the singleton pattern, to access it, use the `JUMPMultiplayer.GameClient` property (which refers to `Singleton<JUMPGameClient>.Instance`.

You use the `JUMPGameClient` to send commands to the server; to do so, just use the `SendCommandToServer` operation. To define your own commands, see [`JUMPCommand`](#jumpcommand).
```c#
JUMPMultiplayer.GameClient.SendCommandToServer(new myCommand());
```

The `ConnectToServer` operation and `OnSnapshotReceived` event are used internally by `JUMPMultiplayer`, you don't need to worry about them :)

#### JUMPGameServer
The `JUMPGameServer` is managed by the `JUMPMultiplayer` class, you don't interact with it directly.
`JUMPMultiplayer` uses a singleton `JUMPGameServer` insance to start the game, process client commands and send snapshots to the client.

The `JUMPGameServer` will send a numbe of snapshots to the client per second that can be customized setting the `JUMPOptions.SnapshotsPerSec` property, the default is 3 snapshots per second.

The `JUMPGameServer` is designed to interact with your custom Server Engine - just implement the [`IJUMPGameServerEngine`](#ijumpgameserverengine) interface and set the `GameServerEngineTypeName` property of a `JUMPMultiplayer` instance with Stage [`Stages.Master`](#stagesmaster) (or a *JUMPMultiplayerMaster* prefab).

#### IJUMPGameServerEngine
The `IJUMPGameServerEngine` interface allows you to customize the Server Engine for your multiplayer game.
An instance of your Server Engine will be hosted by the [Master Client], all the communication between client and server is being taken care of by **JUMP**, you can focus on implementing your game logic.

Here is the `IJUMPGameServerEngine` interface:
```c#
public interface IJUMPGameServerEngine
{
    void StartGame(List<JUMPPlayer> Players);
    void Tick(double ElapsedSeconds);
    void ProcessCommand(JUMPCommand command);
    JUMPCommand CommandFromEvent(byte eventCode, object content);
    JUMPCommand_Snapshot TakeSnapshot(int ForPlayerID);
}
```

###### `void StartGame(List<JUMPPlayer> Players)`
**JUMP** will call StartGame when the `JUMPMultiplayer` is in the `Master` stage and the player joins (or creates) a Room, right before calling `OnGameRoomConnect`.
In this operation, you want to initialize your game state, using the information on the `Players` list to save the list of players that are in the game.

For example, the [DiceRoller Custom Server](#diceroller-customs-erver) intializes its own GameState and saves the players using a custom DiceRollerPlayer class.

###### `void Tick(double ElapsedSeconds)`
On the Master Server, `JUMPGameServer` calls `Tick` every frame update to make your game progress forward.
Do anything time related in this operation; don't bother sending Snapshots, this is automated for you with the `TakeSnapshot` operation.

For example, the [DiceRoller Custom Server](#diceroller-customs-erver) counts down its 30 seconds timeout for the game, after that the game is over.

###### `void ProcessCommand(JUMPCommand command)`
This is where you process your custom commands that the client sends.

See [JUMPCommand](#jumpcommand) for how to define your own commands and the [DiceRoller Custom Server](#diceroller-customs-erver) for an example of definition and use of a custom command.

###### `JUMPCommand CommandFromEvent(byte eventCode, object content)`
`JUMPGameServer` needs a way to find out if the Photon Event that it just received from the client comes from your Game Client and carries your custom command, in order to do so, you can implement the `CommandFromEvent` function, checking if the `eventCode` is one of your custom operations' one.

See, the [DiceRoller Sample](#diceroller-customs-erver) for an example on how to write this function.

###### `JUMPCommand_Snapshot TakeSnapshot(int ForPlayerID)`
`JUMPGameServer` will send Snapshots automatically to your clients; the `TakeSnapshot` function is where you can customize the Snapshot.

See, the [DiceRoller Custom Server](#diceroller-customs-erver) for an example on how to write this function.

#### JUMPCommand
`JUMPCommand` is a base class that allows **JUMP** to define commands and to send them and receive them using the Photon Events system. You can define your own custom commands with little coding.

`JUMPCommand` uses the `CommandEventCode` property as Photon Event Code, this is a byte variable, in your game you can use any value from `0` to `189`.
The `CommandData` is an object array used to store and retrieve the data for your command, and that can be easily serialized with Photon messages. Only basic types are allowed as Command properties to store in `CommandData`, for more background information, see [Serialization In Photon](#https://doc.photonengine.com/en/realtime/current/reference/serialization-in-photon)

Custom `JUMPCommand`s typically need two constructors, one used for reconstruction of the Command when received from Photon, and one used to initialize the CommandData before sending it to the server.

`JUMPMultiplayer` uses `JUMPCommand_Connect` as a custom command used to connect the `GameClient`:
```c#
public class JUMPCommand_Connect : JUMPCommand
{
    public const byte JUMPCommand_Connect_EventCode = 191;

    public int PlayerID { get { return (int)CommandData[0]; } set { CommandData[0] = value; } }

    public JUMPCommand_Connect(int playerID) : base(new object[1], JUMPCommand_Connect_EventCode)
    {
        PlayerID = playerID;
    }

    public JUMPCommand_Connect(object[] data) : base(data, JUMPCommand_Connect_EventCode)
    {
    }
}
```

See, the [DiceRoller Custom Server] for other examples on wiritng your own custom Commands.

#### JUMPCommand_Snapshot
Then the `JUMPGameServer` sends `JUMPCommand_Snapshot` periodically to the client.
`JUMPCommand_Snapshot` is a `JUMPCommand` that has two property already set: the `JUMPSnapshot_EventCode` and the `ForPlayerID` property.

You can define your own Snapshot, by inheriting from the `JUMPCommand_Snapshot` class - 

See, the [DiceRoller Custom Server] for other examples on wiritng your own custom Snapshots.

#### JUMPPlayer
`JUMPPlayer` is a simple class used to store basic information like `PlayerID` and `IsConnected`.
You can extend the `JUMPPlayer` with your own properties, like Score for example and keep them in your server state.

See, the [DiceRoller Custom Server] for an example on extendig the `JUMPPlayer` class.

#### JUMPOptions
You can set a few options with the `JUMPOptions` class - here are the options with their defaults:

```c#
public static class JUMPOptions
{
    public static string GameVersion = "0.1";
    public static byte NumPlayers = 2;
    public static int DisconnectTimeout = 10 * 1000;
    public static int SnapshotsPerSec = 3;
}
```

Note that `DisconnectTimeout` is set to 60 seconds if the build is in Debug mode.

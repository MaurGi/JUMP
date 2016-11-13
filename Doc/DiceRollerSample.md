# DiceRoller Sample
DiceRoller is a simple example of how to use **JUMP**.
It is made of five scenes, a custom Server Engine and a Game Manager for the play scene.

#### Connection Scene
The Connection Scene has one instance of the **JUMPMultiplayerConnection** [prefab](#jumpmultiplayerprefabs) (as a reminder,  this is a behaviour that has a `JUMPMultplayer` component, with the `Stage` set as `Connection`).

The only event handled by the scene the `OnMasterDisconnect`, in which we load the Master Scene.

The Scene also uses the UI Prefab **JUMPStatusConnection** that displays the status of the connection with Photon.

#### Master Scene
The Master Scene has has one instance of the **JUMPMultiplayerMaster** prefab and can handle online and offline play with bots.

The scene handles the `OnMasterDisconnect` event, that goes back to the Connection Scene to try and reconnect once.

The scene sets the `GameServerEngineTypeName` variable to `"DiceRollerSample.DiceRollerEngine"` to create a custom server engine - for details see the [DiceRoller Custom Server].

The scene sets the `BotTypeName` variable to `"DiceRollerSample.DiceRollerBot"` to create a custom bot - for details see the [DiceRoller Bot].

For the online play:
* The scene has a **JUMPButtonStartMatchmaking** a simple text button that is enabled when we are connected to the Photon Master Server; if the user clicks the button, then we invoke the `Matchmake` operation on the *JUMPMultiplayerMaster** prefab.
* The scene then responds to the `OnGameRoomConnect` and loads the Game Room Scene

For the offline play:
* The scene has a **JUMPButtonStartOfflinePlay** if the user clicks the button, then we invoke the `OfflinePlay` operation on the *JUMPMultiplayerMaster** prefab.
* The scene then responds to the `OnOfflinePlayConnect` and loads the Play Scene

The scene uses a few more UI prefabs:
* **JUMPStatusOnline** that displays if the client is online (connected with Photon) or offline
* **JUMPStatusPlayers** that displays the number of players connected to Photon

#### Game Room Scene
The Game Room Scene has one instance of the **JUMPMultiplayerGameRoom** prefab.

The scene handles two events:
* `OnGameRoomDisconnect` that goes back to the Master Scene.
* `OnPlayConnect` that loads the Play Scene

The scene uses a few more UI prefabs:
* **JUMPStatusGameRoom** that displays how many players are in the room.
* **JUMPButtonCancelGameRoom** which is enabled during this phase of the UI Flow. If the user clicks the button, then we invoke the `CancelGameRoom` operation on the **JUMPMultiplayerGameRoom** prefab.

#### Play Scene
The Game Room Scene has two instance of the **JUMPMultiplayerPlay** prefab:
* `JUMPMultiplayerPlay` is the one that handles the online play 
* `JUMPMultiplayerOfflinePlay` is the one that handles the offline play:

The play scene manager (`DiceRollerGameManager`) activates one of the prefabs, based on the OfflinePlayMode:
```
    void Start()
    {
		...
        OfflinePlayManager.gameObject.SetActive(JUMPMultiplayer.IsOfflinePlayMode);
        OnlinePlayManager.gameObject.SetActive(!JUMPMultiplayer.IsOfflinePlayMode);
    }
```

For both online and offlinePlay, the scene handles the same two events (fired by the active JUMPMultiplayer prefab):
* `OnPlayDisconnect` that goes back to the Master Scene.
* `OnSnapshotReceived` that handles the Snapshots form the server via the DiceRollerGameManager - see [DiceRoller Custom Server] for details

The scene uses a few more UI prefabs:
* **JUMPButtonQuitPlay** hich is enabled during this phase of the UI Flow. If the user clicks the button, then we invoke the `QuitPlay` operation on the **JUMPMultiplayerPlay** prefab.

### DiceRoller Custom Server
DiceRoller is a simple game, two players roll a dice and try to score the most points in 30 seconds.
To function, DiceRoller needs the Scenes and two additional components:
* Gustom Server
* Game Manager script for the Play Scene

> Note: for simplicity of implementation, the action of rolling a dice is non-authoritative: the clients decide the outcome of rolling the dice autonomously; this helps keeping the DiceRoller sample code very straightforward and simple to understand. In your game you might want to have the server take the decisions on the outcome of player actions like this.

#### Custom Server
To create a custom server, DiceRoller implements the [`IJUMPGameServerEngine`](#ijumpgameserverengine) interface:

##### DiceRollerCommand_RollDice
DiceRoller defines a custom command for rolling the dice, this inherits from `JUMPCommand` and extends it:

```c#
public class DiceRollerCommand_RollDice : JUMPCommand
{
    public const byte RollDice_EventCode = 100;

    public int PlayerID { get { return (int)CommandData[0]; } set { CommandData[0] = value; } }
    public int RolledDiceValue { get { return (int)CommandData[1]; } set { CommandData[1] = value; } }

    // Create a command to send with this initializer
    public DiceRollerCommand_RollDice(int playerID, int rolledDiceValue) : base(new object[2], RollDice_EventCode)
    {
        PlayerID = playerID;
        RolledDiceValue = rolledDiceValue;
    }

    // Create a command when receiving it from Photon
    public DiceRollerCommand_RollDice(object[] data) : base(data, RollDice_EventCode)
    {
    }
}
```

Note the definition of the event code: `RollDice_EventCode`, the use of the standard `PlayerID` property as the first value in the CommanData array and the definition of two constructors: `DiceRollerCommand_RollDice(object[] data)` used to create the command from the Photon message and `DiceRollerCommand_RollDice(int playerID, int rolledDiceValue)` used to send the message.

##### DiceRollerPlayer
DiceRoller defines a custom Player to store the game state in [`DiceRollerGameState`](#dicerollergamestate).

```c#
public class DiceRollerPlayer : JUMPPlayer
{
    public int Score = 0;
}
```

Note the inheritance from `JUMPPlayer` and the addition of the only property that matters in this case, the Score.

##### DiceRollerGameState
DiceRoller custom server defines theits own game state of course - this contains a list of `DiceRollerPlayers`, the time remaining in the game, the stage of the game (can be waiting for the players, playing or complete) and the time remaining when the game is being played:

```c#
public enum DiceRollerGameStages
{
    WaitingForPlayers,
    Playing,
    Complete
}

public class DiceRollerGameState
{
    public Dictionary<int, DiceRollerPlayer> Players = new Dictionary<int, DiceRollerPlayer>();
    public float SecondsRemaining;
    public DiceRollerGameStages Stage;
    public int WinnerPlayerID;
}
```

##### DiceRoller_Snapshot
To communicate the state of the game with the clients, the DiceRoller custom server must define a Snapshot.
The `DiceRoller_Snapshot` class inherits from `JUMPCommand_Snapshot` and so can make use of the array of data `CommandData`, the `JUMPSnapshot_EventCode` and the `ForPlayerID` properties already defined in the base class.

All it needs to do is to define two constructors and use the `CommandData` array to store the Snapshot data.

Note that a Snapshot is different from the GameState because it is aimed to only one of the two players: players should not see each other's data (for example if they have playing cards). 

```c#
public class DiceRoller_Snapshot : JUMPCommand_Snapshot
{
    // ForPlayerID is at CommandData[0]
    public int MyScore { get { return (int)CommandData[1]; } set { CommandData[1] = value; } }
    public int OpponentScore { get { return (int)CommandData[2]; } set { CommandData[2] = value; } }
    public float SecondsRemaining { get { return (float)CommandData[3]; } set { CommandData[3] = value; } }
    public DiceRollerGameStages Stage { get { return (DiceRollerGameStages)CommandData[4]; } set { CommandData[4] = value; } }
    public int WinnerPlayerID { get { return (int)CommandData[5]; } set { CommandData[5] = value; } }

    // Create a command to send with this initializer
    public DiceRoller_Snapshot() : base(new object[6])
    {
    }

    // Create a command when receiving it from Photon
    public DiceRoller_Snapshot(object[] data) : base(data)
    {
    }
}
```

Note how the `DiceRoller_Snapshot()` constructor creates a new array with 6 elements: one for the `ForPlayerID` property used by the `JUMPCommand_Snapshot` base class and five for the custom properties.
Also note how the properties are stored from the second element in the array on, because `ForPlayerID` is stored at element 0.

##### DiceRollerEngine
The `DiceRollerEngine` class implements the `IJUMPGameServerEngine` interface.

It uses an internal variable to hold the state:

```c#
private DiceRollerGameState GameState;
```

The constructor simply initializes the state in the waiting for players mode:

```c#
public DiceRollerEngine()
{
    GameState = new DiceRollerGameState();
    GameState.Stage = DiceRollerGameStages.WaitingForPlayers;
}
```

The `CommandFromEvent` function handles the RollDice custom command:

```c#
public JUMPCommand CommandFromEvent(byte eventCode, object content)
{
    if (eventCode == DiceRollerCommand_RollDice.RollDice_EventCode)
    {
        return new DiceRollerCommand_RollDice((object[]) content);
    }
    return null;
}
```

The `StartGame` operation gets the information about the players and sets the state of the game:

```c#
public void StartGame(List<JUMPPlayer> Players)
{
    GameState = new DiceRollerGameState();
    GameState.SecondsRemaining = 30;
    GameState.Stage = DiceRollerGameStages.Playing;

    foreach (var pl in Players)
    {
        DiceRollerPlayer player = new DiceRollerPlayer();
        player.PlayerID = pl.PlayerID;
        player.IsConnected = pl.IsConnected;
        player.Score = 0;

        GameState.Players.Add(player.PlayerID, player);
    }
}
```

The `ProcessCommand` handles the server state when a client sends a RollDice command (the connect commands are manager automatically by the `JUMPGameServer` class).

```c#
public void ProcessCommand(JUMPCommand command)
{
    if (command.CommandEventCode == DiceRollerCommand_RollDice.RollDice_EventCode)
    {
        DiceRollerCommand_RollDice rollDiceCommand = command as DiceRollerCommand_RollDice;

        DiceRollerPlayer player;
        if (GameState.Stage == DiceRollerGameStages.Playing)
        {
            if (GameState.Players.TryGetValue(rollDiceCommand.PlayerID, out player))
            {
                player.Score += rollDiceCommand.RolledDiceValue;
            }
        }
    }
}
```

The `Tick` operation handles the game clock, when the clock expires, the game state is changed to Coplete and a winner is determined.

```c#
public void Tick(double ElapsedSeconds)
{
    if (GameState.Stage == DiceRollerGameStages.Playing)
    {
        GameState.SecondsRemaining -= (float) ElapsedSeconds;
        if (GameState.SecondsRemaining <= 0)
        {
            int maxscore = 0;
            int winner = -1;
            foreach (var item in GameState.Players)
            {
                if (item.Value.Score > maxscore)
                {
                    maxscore = item.Value.Score;
                    winner = item.Key; 
                }
            }
            GameState.Stage = DiceRollerGameStages.Complete;
            GameState.WinnerPlayerID = winner;
        }
    }
}
```

The `TakeSnapshot` operation is called by `JUMPGameServer` to send the snapshot to a player, so the snapshot is created for that specific player:

```c#
public JUMPCommand_Snapshot TakeSnapshot(int FofrPlayerID)
{
    DiceRoller_Snapshot snap = new DiceRoller_Snapshot();
    snap.ForPlayerID = ForPlayerID;
    snap.MyScore = 0;
    snap.OpponentScore = 0;
    foreach (var item in GameState.Players)
    {
        if (item.Value.PlayerID == ForPlayerID)
        {
            snap.MyScore = item.Value.Score;
        }
        else
        {
            snap.OpponentScore = item.Value.Score;
        }
    }
    snap.SecondsRemaining = GameState.SecondsRemaining;
    snap.Stage = GameState.Stage;
    snap.WinnerPlayerID = GameState.WinnerPlayerID;

    return (JUMPCommand_Snapshot) snap;
}
```

#### Game Manager
The final piece of **DiceRoller** is the `DiceRollerGameManager`, this is the Controller part in the MVC pattern: displaying the information in the user interface (View) and working with the `Snapshot` (Model) received by the `JUMPMultiplayer` class (see [Play Scene](#play-scene)).

'DiceRollerGameManager' is a 'MonoBehaviour' and uses multiple `Text` and `Button` controls to display and control the game:

```c#
public Text MyScore;
public Text TheirScore;
public Text GameStatus;
public Text TimeLeft;
public Text Result;
public Button RollDice;
```

When a snapshot is received, the controls are updated:

```c#
DiceRollerGameStages UIStage = DiceRollerGameStages.WaitingForPlayers;

public void OnSnapshotReceived(JUMPCommand_Snapshot data)
{
    DiceRoller_Snapshot snap = new DiceRoller_Snapshot(data.CommandData);
    GameStatus.text = snap.Stage.ToString();
    MyScore.text = snap.MyScore.ToString();
    TheirScore.text = snap.OpponentScore.ToString();
    TimeLeft.text = snap.SecondsRemaining.ToString("0.");
    UIStage = snap.Stage;
    if (UIStage == DiceRollerGameStages.Complete)
    {
        Result.text = (snap.MyScore > snap.OpponentScore) ? "You Won :)" : ((snap.MyScore == snap.OpponentScore) ? "Tied!" : "You Lost :(");
    }
}
```

The user can roll a dice, this operation will send the custom command to the server using the `JUMPGameClient` singleton:

```c#
public void RollADice()
{
    int score = UnityEngine.Random.Range(1, 6);
    if (RollDice != null)
    {
        RollDice.GetComponent<Text>().text = "Rolled a " + score + " \nroll again.."; 
    }
    Singleton<JUMPGameClient>.Instance.SendCommandToServer(new DiceRollerCommand_RollDice(JUMPMultiplayer.PlayerID, score));
}
```

Finally, the Unity `Start` function is used to initialize the random seed and in the `Update` function we enable the RollDice button only if we are playing:

```c#
// Use this for initialization
void Start () {
    UnityEngine.Random.seed = System.DateTime.Now.Millisecond; 
}

// Update is called once per frame
void Update () {
    RollDice.interactable = (UIStage == DiceRollerGameStages.Playing);
}
```

### DiceRoller Bot

The `DiceRollerBot` is very simple, just rolls a 3 every three seconds!

You can develop deeper strategies having full access to the `DiceRollerEngine` and the full `DiceRollerGameState` (the Bot can distinguish his score from his opponent using the Bot's `PlayerID`, set bv the engine.

Here is the full bot implementation:
```
    public class DiceRollerBot : JUMPPlayer, IJUMPBot
    {
        public int Score = 0;

        private TimeSpan TickTimer = TimeSpan.Zero;
        private TimeSpan CommandsFrequency = TimeSpan.FromMilliseconds(1000 / 0.3);

        public IJUMPGameServerEngine Engine { get; set; }

        public void Tick(double ElapsedSeconds)
        {
            TickTimer += TimeSpan.FromSeconds(ElapsedSeconds);
            if (TickTimer > CommandsFrequency)
            {
                TickTimer = TimeSpan.Zero;

                // Every 3 seconds, roll a 3
                DiceRollerEngine engine = Engine as DiceRollerEngine;

                engine.ProcessCommand(new DiceRollerCommand_RollDice(PlayerID, 3));
            }
        }
    }
```
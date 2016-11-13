# Offline Bots

Offline Bots ara available in the Offline Play state.

To create a Bot, you need to have a class that implements the `IJUMPBot` interface.

Note, that a bot is also a player, so the `IJUMPBot` implements also the 'IJUMPPlayer' interface.

JUMP will create automatically as many bots as the number of players in the game (`JUMPOptions.NumPlayers`) minus one for your human player.

Note: a mix of online players and bots is not supported, you can only have one human player and the rest bots.

### Offline play

To start the offline play with bots, set the `BotTypeName` property with the class name of the bot you want to use and then call the `OfflinePlay` method, when you are in the Master stage.

This will create a list of Bots and trigger the `OnOfflinePlayConnect` event.

At this point you want to load a scene with a JUMPMultiplayer object in OfflinePlay stage.

The client will receive snapshot and you can send commands, like in the online `Play` stage, but in this case, your bots will be invoked and will be sending commands to the server.

You can very easily have a single play scene that handles online and offline play, all you need to do is have a `JUMPMultiplayerPlay` and a `JUMPMultiplayerOfflinePlay` prefabs (disabled) in the play scene, then in the `Activate` function, check the JUMPMultiplayer.IsOfflinePlay property and just set active the relevant one - see the DiceRoller sample for an example.

### Writing a Bot

To facilitate writing engaging Bots, they have full access to the state of the game.
Bots don't use the snapshot, but get a pointer to your Engine, where you usually store your game state, and a Tick from the server:

```c#
public interface IJUMPBot : IJUMPPlayer
{
	void Tick(double ElapsedSeconds);

	IJUMPGameServerEngine Engine { get; set; }
}
```

In the Tick event, you want to access the state on your Engine and have your bots behave accordingly.

In the DiceRoller sample, for example, the Bot simply rolls a 3 every three seconds. Note that you can access `(DiceRollerEngine) Engine` and have full access to your state, for example you want your bot to try and catch up with the player and have a higher chance to roll a high number if the bot is behind.
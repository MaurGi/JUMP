# JUMP (Just Unity Multiplayer with Photon)

**JUMP** is a library that facilitates writing **simple multiplayer games** using **[Unity 3D]** 
and **[Photon Unity Networking]**.

I was working on a multiplayer game and found myself in trouble trying to handle events from 
Unity and Photon at the same time; ending up with fragile code - so I worked to 
streamline and simplify the scenario and build a reusable library.

Out of the box, **JUMP** Provides:
- A simple flow for connecting to the server, matchmaking and starting a multiplayer game
- Authoritative local Server (using Photon Master Client)
- Streamlined Client-Server communication
- Custom Client Commands and Server Snapshots
- Sample Project and Prefabs to get started in 5 minutes
- Full source code access and great customization.

#### Current Version
[0.2.0] - 2016-05-05

For details, see [Changelog](https://raw.githubusercontent.com/MaurGi/JUMP/master/CHANGELOG.md)

### Table of Content
#### Table of Content
* **[How To Install](#how-to-install)**
* **[Scenario](#scenario)**
* **[Object Model](#object-model)**
* **[Offline Bots](#offline-bots)**
* **[DiceRoller Sample](#diceroller-sample)**
* **[Testing JUMP](#testing-jump)**
* **[Features to Add](#features-to-add)**
* **[License](#license)**

# How to Install
You can get the source code here or download **JUMP** from the [Unity Asset Store][JUMPAsset].

#### Prerequisites
**JUMP** works with [Unity 5](https://unity3d.com/5) and requires [Photon PUN](https://www.assetstore.unity3d.com/en/#!/content/1786) to function properly:

1. Install [Photon PUN Free](https://www.assetstore.unity3d.com/en/#!/content/1786) or [Photon PUN+](https://www.assetstore.unity3d.com/en/#!/content/12080)
2. Configure your Photon Application ID - see [Initial Setup](https://doc.photonengine.com/en/pun/current/getting-started/initial-setup)

For more information about configuring PUN, see [PUN Setup](https://doc.photonengine.com/en/pun/current/getting-started/initial-setup).

> Note: JUMP was tested with Unity version 5.3.4 and Photon PUN Free version 1.67

#### Option 1: Unity Asset Store
The [Unity Asset Store][JUMPAsset] package includes the [DiceRoller](#diceroller-sample) sample project.

* Download **JUMP** from the [Unity Asset Store][JUMPAsset].
* Add all the scenes in the `DiceRollerSample/` to the Unity build
* Open the `DiceRollerConnection` scene and start Unity.
* The DiceRoller sample should go to the Matchmake scene in connected mode.

> Note: To test multiplayer games in Unity, you will need to run two copies of the game, so that they can connect to each other. The best way, is to build a copy of the game (File/Build & Run) for one player and start Unity debugging for the second player. See this Unity [forum post](http://answers.unity3d.com/questions/214802/how-to-test-your-multiplayer-game.html) for more details.
 
#### Option 2: From GitHub source code
In alternative to use the Asset store, you can create a folder in the unity project (under Assets) and copy all the files from the `JUMP Multiplayer\` folder.
Then you can use the [Object Model](#object-model) and the prefabs.

# Scenario
The scenario supported by JUMP is very simple and designed with mobile multiplayer games in mind:
* [Two Players networking](#two-players-networking)
* [Basic matchmaking](#basic-matchmaking)
* [Streamlined UI flow](#streamlined-ui-flow)
* [Custom server](#custom-server)
* [Client-Server communication](#client-server-communication)

#### Two Players networking
JUMP automates building Photon game rooms, easy.
 
*Note:* While it supports more than two players, we tested JUMP with the two players scenario

#### Basic Matchmaking
JUMP uses Photon [Random Matchmaking](https://doc.photonengine.com/en/realtime/current/reference/matchmaking-and-lobby) without the use of the Photon Lobby making the UI and the object model much simpler to use compared to the use of the Lobby.

#### Streamlined UI flow
JUMP supports up to five different Unity scenes:
* Connection
* Home Page (Master)
* [Room] Waiting for players
* [Room] Playing the game

*Note:* support for multiple levels can be added to the Playing the game scene. 

![](https://raw.githubusercontent.com/MaurGi/JUMP/master/Doc/UIFlow.png)

#### Custom Server
JUMP cannot provide a dedicated remote server using [Photon Unity Networking] or [Photon Cloud], so it uses the **Host** model, where one of the clients also hosts the Game Server (sometimes called **Local Server**) 

Photon provides support for this with the concept of [Master Client].

#### Client-Server Communication
JUMP supports the concept of an Authoritative or Semi-Authoritative server using a Command/Snapshot Client-Server communication:
* The Client sends Commands to the Server
* The Server sends Snapshot to the Client

The model is similar to the one explained by [Fast Paced Multiplayer] [Gambetta], but we use the term **Command** instead of Actions and **Snapshot** instad of New State

![](https://raw.githubusercontent.com/MaurGi/JUMP/master/Doc/ClientServer.png)

> The slanted lines indicate that there is lag between the client and server communication, see [Fighting Latency on Call of Duty III] [CallOfDuty]


While it is possible to optimize the client/server communication in terms of reliability and space utilized with delta compression of snapshots and queues of commands, JUMP simply uses 
[Photon Reliable UDP](https://doc.photonengine.com/en/onpremise/current/getting-started/photon-server-intro) to communicate, it is a good balance of reliability and ease-of-use.

#### References
JUMP Multiplayer with (Semi)Authoritative Server model is based on lots of literature on multiplayer games:
* [Fast Paced Multiplayer] [Gambetta]  (Gambetta)
* [What every programmer needs to know about game networking](http://gafferongames.com/networking-for-game-programmers/what-every-programmer-needs-to-know-about-game-networking/) (Gaffer on Games)
* [Fighting Latency on Call of Duty Black Ops III] [CallOfDuty]  (GDC 2016)
* [Building a Peer-to-Peer Multiplayer Networked Game](http://gamedevelopment.tutsplus.com/tutorials/building-a-peer-to-peer-multiplayer-networked-game--gamedev-10074)
* [Unity3D Multiplayer Game Development](http://www.pepwuper.com/unity3d-multiplayer-game-development-unity-networking-photon-and-ulink-comparison/)
* [How can I make a peer-to-peer multiplayer game?](http://gamedev.stackexchange.com/questions/3887/how-can-i-make-a-peer-to-peer-multiplayer-game/3891#3891)
* [Unity Networking](http://docs.unity3d.com/Manual/UNetConcepts.html)

---

## Object Model
See **[Object Model](doc/JUMPOjectModel.md)**

## Offline Bots
See **[Offline Bots](doc/OfflineBots.md)**

## DiceRoller Sample
See **[DiceRoller Sample](doc/DiceRollerSample.md)**

## Testing JUMP
See **[Testing JUMP](doc/MiscDoc.md#testing-jump)**

## Features to Add
See **[Features to Add](doc/MiscDoc.md#features-to-add)**

# License

Unless stated otherwise all works are Copyright &copy; 2016 [Juiced Team LLC.](http://www.juicedteam.com)

And licensed under the [MIT License](https://raw.githubusercontent.com/MaurGi/JUMP/master/LICENSE.txt)

### Donate
If you want to donate, you can simply [purchase the JUMP package in the Unity Asset Store][JUMPAsset].

### Contribute
We are not ready to accept pull requests at the time, we are considering that for the future.
If you are interested in contributing, file a bug and we will consider your request.
All contributions will be voluntary and will grant no rights, compensation or license, you will retain the rights to reuse your code.

### Attribution
Photon, Photon Engine, PUN: &copy;2016 [Exit Games](https://www.photonengine.com)&reg; 

Unity 3D, Unity Engine: &copy;2016 [Unity Technologies](http://unity.com)

[Unity 3D]: <http://unity3d.com/unity>
[Photon Unity Networking]: <http://www.photonengine.com/PUN>
[Photon Cloud]: <http://www.photonengine.com/en-US/Realtime>
[Master Client]: <http://doc.photonengine.com/en/pun/current/tutorials/tutorial-marco-polo>
[Photon Lobby]: <http://doc.photonengine.com/en/realtime/current/reference/matchmaking-and-lobby>
[Gambetta]:<http://www.gabrielgambetta.com/fpm1.html>
[TODOPicture]:<https://upload.wikimedia.org/wikipedia/commons/2/21/OpenGL_Tutorial_TODO.png>
[CallOfDuty]:<http://schedule.gdconf.com/session/fighting-latency-on-call-of-duty-black-ops-iii>
[JUMPAsset]:<http://u3d.as/uRe>

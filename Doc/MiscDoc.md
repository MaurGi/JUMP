# Testing JUMP
Given the fact that Unity does not allow the same project to be opened in two different instances of the Unity Editor and that it can only run one scene at a time, it is very hard to create test automation and to test **JUMP**.
We have done manual testing, but finding a way to automate some of this test would be ideal.
We thought about mocking, but then it would require to mock the behaviour of Photon, making assumptions that might not be matched in the real case and invalidating the tests.

So we worked with manual tests so far, here are the test cases we tried:

##### Start the game
* Start a Game from the Connection Scene -> the Game should end in the Master  scene with Offline mode on.
* Start a Game from any other scene -> the Game should go back to the connection Scene and then end in the Master scene with Offline mode on.
* Start a Game with no connection from any Scene -> the Game should end in the Master scene with Offline mode on.

##### Disconnect from the game
* When on any scene, disconnect the network -> the Game should go back to the connection Scene and then end in the Master  scene with Offline mode on.  

##### Matchmake
* When on the Master scene, press Matchmake -> the Game should go to the Matchmake screen then the Game should go to the Game Room waiting scene; if there is at least another player in matchmaking, then the game should go to the Play scene.
* When on the Matchmake scene if the user press Cancel Matchmaking -> the Game should go back to the main scene; if there is at least another player in matchmaking, then the game should continue on to the Play scene.

##### Game Room waiting
* When on the Game Room waiting scene, if the user press Cancel -> the game should go back to the Master scene

##### Play
* When on the Play scene, if the user press Quit -> the game shouls go back to the Master scene


# Features to add
* Unit test, or at least automation test (with Unity allowing a single window and a single instance this is hard)
* Support for bots when there are not enough players online (users in a room by himself for too long)
* Improved reliability and speed with delta compression of Snapshots and list of not-acknowledged Commands.
* Matchmaking not random but with customizable criteria (SQLLobbies)
* Source Code comments on public variables and methods?

# JUMP Change Log

## [0.1.1] - 2016-04-30
- Initial Release

## [0.1.2] - 2016-05-05
### Added
- Added JUMPMultiplayer.PlayerID property to facilitate sending and receiving messages.
- Closing Photon connection now when application is quitting.
- Introduced LogLevel.Verbose

### Changed
- Fixed a bug when Unity is quitting on singleton
- Improved logging in some error conditions
- Bug fixing

## [0.2.0] - 2016-06-02
### Changed
- Removed the step that joins to the Photon Lobby, now you can join the game straight from the Server - easier user flow and object model
- Bug fixing

## [0.3.0] - 2016-11-13
### Added
- Bots for offline play support

### Changed
- Updated to support Photon 1.76

### Object Model additions:
- New IJUMPBot, IJUMPPlayer interfaces 
- New JUMPMultiplayer.GameClient property for easy access to Singleton<JUMPGameClient>.Current
- New JUMPMultiplayer.GameServer property for easy access to Singleton<JUMPGameServer>.Current
- New JUMPMultiplayer stage: OflinePlay, methods OfflinePlay and properties: IsOfflinePlayMode, BotTypeName, OnOfflinePlayConnect
- New JUMPMultiplayerOfflinePlay, JUMPButtonStartOfflinePlay prefab

### Object Model changes:
- JUMPMultiplayer.IsOffline deprecated, use JUMPMultiplayer.IsPhotonOffline instead

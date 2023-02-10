@echo off
echo Installing Library
SET DLL_SOURCE="..\EnemyRandomizerDB\bin\Library\EnemyRandomizerDB.dll"
SET MOD_DEST="K:\SteamLibrary\steamapps\common\Hollow Knight\hollow_knight_Data\Managed\Mods\EnemyRandomizer"
echo Copying build from
echo %DLL_SOURCE%
echo to
echo %MOD_DEST%
copy %DLL_SOURCE% %MOD_DEST%
SET XML_SOURCE="..\EnemyRandomizerDB\Library\XMLResources\EnemyRandomizerDatabase.xml"
SET MOD_DEST="K:\SteamLibrary\steamapps\common\Hollow Knight\hollow_knight_Data\Managed\Mods\EnemyRandomizer"
echo Copying XML database from
echo %XML_SOURCE%
echo to
echo %MOD_DEST%
copy %XML_SOURCE% %MOD_DEST%
PAUSE
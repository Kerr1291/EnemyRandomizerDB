# EnemyRandomizerDB

So all versions of enemy rando until now have been using a mostly hand-made database to look up enemies in the game. This has resulted in a giant headache as there always seems to be some missed exception here or there, so created a database generator "mod" to parse out all the desired stuff from the game and create a clean/uniform way to go from  "Hollow Knight Game Object" --> "Known Database Object" -> "Enemy Randomizer Object"

Basically, the database builds itself on load, like what the enemy randomizer does now, but it alone handles the responsibility of parsing names and creating sensible mappings for things in hollow knight, in addition to whatever required modifications should be done to make the game objects functional.

Then this mod is used to save/serialize out the data into the database format you want. The project is then rebuilt as a library which you may include with your mod and is responsible for loading/managing the xml data and acting as an intermediary between your mod and hollow knight.

That way you may just do your mod's logic through the database without having to guess/hack at the game objects in hollow knight directly.

In addition, the database can build out a structure with all the scenes and enemies in the game. That's already been done if you want it and is included in this repo under the SceneData folder.

Using these files would make it much easier to develop more interesting randomization logic, since you could query scenes and game objects by parsing these resources instead of having to load the scenes directly.

I hope someone finds this useful :)

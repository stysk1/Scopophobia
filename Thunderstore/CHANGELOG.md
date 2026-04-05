# 1.3.4
## v80 Spawn Fix
+ Fixes an error thrown when grabbing enemy data for the current Level. (Reported by @Glacius on Discord) 
# 1.3.3
## V80 Build
+ This is an early V80 Beta build. Please report issues in the Lethal Company Modding Discord, or to me directly on Github.
+ **NOTE** There are possible known issues with this release, as I havent had a good opportunity to debug everything properly. Previously reported issues are still possible, and I'll work out these bugs once and for all at some point soon.
# 1.3.2
## Minor Bug fixes
+ Fix a regression with the Shy Guy Painting triggering more than once, will now only trigger if it hasn't already triggered.
+ Fixed an issue with KillSFX looping over and over again
+ removed Debugging code for Triggering the painting on Interact, this was only ever for testing means.
+ fixed an issue with Painting Spawn chance.
+ Maybe fixed targetting when a player dies while being chased by shy guy? Not sure
# 1.3.1
## v73 Fixes 2
+ Fix Mineshaft code so it actually runs and doesn't soft lock anymore (lol)
+ Add Belt Bag Trigger for Shy Guy Painting (Requires BagConfig by MattyMatty)
+ Fix Audio Bug when shy guy sits down
+ Fix Logging errors when landing on a Planet.
+ Loot bugs / Baboon Hawks can no longer trigger the painting (again, lol). This might return as a config.
# 1.3.0
## v73 Fixes
+ Fix RPC calls for Clients
+ Revert to LethalLib temporarily, Im waiting for updated Unity Project Patcher to make any more major changes. I corrupted my current Unity Install, and since a new version is coming, its easier to wait to update things.
# 1.2.91
## Quick Hotfix
+ Fix for configs resetting spawn weight to 0 (Thanks Lunxara for letting me know.)
+ May fix targetting multiple players again. I havent had enough time to test with others yet, but also shouldn't cause an issue even if it doesn't work.
# 1.2.9
+ Quick Update for v73 Support.
+ Switch from LethalLib to DawnLib. (Basic Implement, Guaranteed to change in future) Thanks @Xu for answering my stupid questions.
# 1.2.8
## Here we are with more fixes
+ Fix Audio RPC issue (Thanks Lunxara)
+ Fix for shy guy randomly walking to the ship, and having a meet and greet (lmao)
# 1.2.7
## Navigation Fixes
+ Fix a revert in navigation (Fixes Wesley Moons) (Thanks @SamBammers, @Lunxara)
+ Fix mistake in Escape mechanic that caused navigation to fail occasionally on certain moons.
+ Fix Wesleys Interiors escape mechanism, Might fix other broken interiors too.
+ Fix Painting interaction triggering more than once per player. (Thanks, @Zeta)
# 1.2.6
## Small fixes
+ Fix Redacted from happening at Oxyde, Galetry, and Gordion properly (Thanks, Xu).
+ Config option for Painting Chance (Lunxara Request)
# 1 2.5
## MORE PAINTING FIXES 
+ Fix Error caused by Painting Scrap
+ Fix Spawn mechanic with Painting
+ Hopefully stop breaking everything else along with this release
# 1.2.4
## Fixes again
+ Hopefully fix issues with paintings
# 1.2.3
## Painting Configs
+ Add Config options for The Shy Guy Painting. Customise the spawn in name, if the name is hidden at start, and upped the spawn rate a little.
+ Painting now works on random chance, if you happen to be above the threshold, something bad happens. if below, you're safe for the round. 
+ Fixed the Painting so it only triggers once per player
 # 1.2.2
## Fix silly mistake
- Fix mistake that meant shy guy would spawn on the company building when the painting was interacted with, oops. (I thought i'd already done this)
# 1.2.1
## The Scrap Update
- A new painting Item has been added, with two variants. Beware picking these up, previous scrappers have not returned..
- More to come! 
# 1.2.0
## The Settings Rewrite, and more
+ Fix Escape method so that Shy Guy can now use Fire Exits (Early, there might be bugs, but he should navigate to whatever door is closer, fire exit or main).
+ Completely replaced the settings to now use ShyGuySettings by Zehs (Thanks for the MIT, and the awesome mod Crit!)
+ Fix a few extra bugs in his pathing, and clean up the code a bit.
# 1.1.9
## AI Fixes (Again)
+ Fix broken AI when indoors (Thanks to both @Arterra & @SamBammers for the report)
# 1.1.8
## Asset Fixes
+ Fix misdirected Sounds when players are Killed (Thanks Lunx)
+ Hopefully Fix MapRadar glitch happening to players
# 1.1.7
## V70 Bugfixes, and more bugs!
+ Add The ability for Shy Guy to Break into the Ship (Complete with Custom animation)
+ Finally fix sound channel issues
+ Hopefully fix all missing sounds playing
+ Recreated the Scopophobia Asset Bundle for easier Future additions (there may be bugs).
+ This is considered a new major release, please report any issues to the Github, or in the Thread in The Lethal Company Modding Discord.
# 1.1.6
## Code Improvements
+ Shy Guy will now kill players hiding at the top of the Elevator (Whoops).
+ Improved the Elevator Script, will only run when Shy guy is inside now.
# 1.1.5
## Fixes
+ Fix Shy Guy AI. Now he will hunt and Kill, and Ride the Elevator to do it! <3
# 1.1.4
## Updated for v62 & Bug Fixes
+ updated Assemblies to v62
+ Fixed Invisible Shy Guy spawns
+ Fixed Sound Channel Issues (I Hope)
+ Added Support for New Mineshaft Interior (Shy guy on Elevator)
+ Added a New Config Option for v62 to disable Spawn Patching, you ***MUST*** use another mod like LethalQuantities or LethalLevelLoader to add Shy Guy to monsters. This is disabled by default.
# 1.1.3
## Updated for v55
+ This version has been updated and Patched for v55 by TheUnknownCod3r. Please report any issues on their GitHub: https://github.com/TheUnknownCod3r/Scopophobia.
+ Updated KillPlayerRPC
# 1.1.2
## Patched for v50
+ This version has been updated and patched for v50 by TheUnknownCod3r. Please report any issues on their GitHub: https://github.com/TheUnknownCod3r/Scopophobia.
# 1.1.1
## Spawn Config (but fixed now!)
+ Fixed issues with spawn config affecting other enemies. (i very much misunderstood what masked enemy overhaul does)
# 1.1.0
## Spawn Config!
+ Figured out how to make a changelog!
+ Added lots of config options which determine how the Shy Guy spawns (THANK YOU MASKED ENEMY OVERHAUL!!!)
+ Added a switch to the bloody variant of the Shy Guy's material when killing a player (optional)
+ The Shy Guy can now ***Spawn outside***!
+ Fixed issues with rate of spawning.
+ Added config for face trigger grace period (default 0.5s)
+ Added config for other awesome stuff I'm probably forgetting.
# 1.0.2
+ Added the Bloody Textures config and fixed some behavioral issues.
+ Added face trigger range config.
# 1.0.1
+ Fixed multiplayer (forgot to netcode patch LOL).
# 1.0.0
+ Initial official release
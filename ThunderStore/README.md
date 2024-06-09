# SCP-956 (The Pinata)

## Description

Adds SCP-956 to the game along with a few other SCP's (more to come).

## Features

- Player Age: A random age is chosen for every player at the start of each game.
- SCP-956: A Pinata that hates children.
- Behaviors:
	= Default intended behavior: SCP-956 will activate whenever someone under the age of 12 stays within its activation zone.
	- SecretLab: Works similar to the game SCP: Secret Lab. Candy will give random effects (coming soon) and SCP-956 will activate anyone holding candy.
	- Random Age: Everyone is assigned a random age at the beginning of the game and its possible to be a child.
	- All: SCP-956 will activate when *anyone* stays within its activation zone, regardless of age.
- SCP-559: A mysterious cake that can change your age.
- Candy: Mysterious candy.
- SCP-330: A dangerous bowl of candy
- Custom Candy Effects: See section below for how to use

## Custom Candy Effects (Experimental)

There will be a config for each piece of candy (except rainbow and pink). For each candy you can customize it to give many different status effects based on the config.
An example on how to format the config entry is shown in the config as the default value. You need to list each effect, followed by its parameters like this in this format: Effect:Param1,Param2;
It is case sensitive. "stackable" means its percentage can be increased when running that effect again. "timeStackable" means its time can be added onto to prolong the effect.
A list of all available effects and their parameters are listed below:

- HealPlayer:HealthAmount(int),Overheal(bool);
- RestoreStamina:Percentage(int);
- HealthRegen:HPPerSecond(int),Seconds(int);
- StatusNegation:Seconds(int),TimeStackable(bool);
- DamageReduction:Seconds(int),Percentage(int),TimeStackable(bool),Stackable(bool);
- InfiniteSprint:Seconds(int),TimeStackable(bool);
- IncreasedMovementSpeed:Seconds(int),Percentage(int),TimeStackable(bool),Stackable(bool);

## Upcoming Plans

- More animations
- A flamethrower or other fire related tools to kill SCP-956

## Known Issues

- When a player becomes targeted by SCP-956 and player is facing a wall, SCP-956's navigation can be bugged
- When shrunk player uses terminal, cant see the full screen
- When player is *overhealed* and takes damage, resets health to 100 rather than taking off damage amount
- When player loses their hands from SCP-330, their hands are still visible
- Bugs out when landing on a moon

## Support and Feedback

You can find my thread here in the modding discord: https://discord.com/channels/1168655651455639582/1244167215876407337. Please report any bugs or issues here and ill try my best to fix them. Suggestions are also very welcome! You can also report them on the github page here: https://github.com/snowlance7/SCP956

## Credit

- Model "Cake" (https://skfb.ly/6RsDx) by nodoxi is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).
- All other models from SCP: Secret Lab
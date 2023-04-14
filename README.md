# Classicube-MCGalaxy-Guns-Plus-Plugin
Guns Plus is a MCGalaxy plugin that add new and better guns to Classicube and more

+7 New weapons  
+3 New Teams  
+Team Respawning  
+2 Gamemodes
+Health  

**To activate Gun Plus in your level put +gunsplus in the level MOTD**  
To change your gun do /gunplus [gun name]  
To change your team do /gunplus [team name]  
more in /help gunplus  

You can also add your own guns
Go to ```createBullet``` function and make a new ```else if``` branch with ```pl.Extras.GetString("AMMOTYPE") == [name of your gun]```
``` diff 
- NAME OF YOUR GUN MUST BE IN UPPERCASE 
```
Example: ```else if (pl.Extras.GetString("AMMOTYPE") == "SUPERGUN") {```
Now give it stats:  
**pl.Extras["FIRERATE"]**: The firerate of the weapon  
**output.block**: The id of the block that will be shown as the bullet  
**output.damage**: The damage of 1 bullet  
**output.killMessage**: Message that will be shown when you kill someone (If the kill message is " /--- " in game it will look like ```Rulja1234 /--- xX_DeathSlayer_Xx```)  
**output.xSpread**: What is the Horizontal spread of the bullets  
**output.ySpread**: What is the vertical spread of the bullets  
**output.reach**: How many blocks can the bullet go before disappearing
**output.numBullets: How many bullets does the weapon shoot  

*Here is how it all should look like:*  

*else if (pl.Extras.GetString("AMMOTYPE") == "SUPERGUN") {  
&emsp;pl.Extras["FIRERATE"] = 100;  
&emsp;output.block = Block.FromRaw((ushort)767);  
&emsp;output.damage = 100;  
&emsp;output.killMessage = "8=D ";  
&emsp;output.xSpread = 0;  
&emsp;output.ySpread = 0; 
&emsp;output.reach = 10000; 
&emsp;output.numBullets = 10;  
}*  
Now add same name that you put in argument for ```else if``` to ```gunNames```
and reload the plugin now your gun should be in game

# Classicube-MCGalaxy-Guns-Plus-Plugin
Guns Plus is a MCGalaxy plugin that add new and better guns to Classicube and more  

+7 New weapons  
+Teams  
+4 Gamemodes  
+Health  

**To activate Gun Plus in your level put +gunsplus in the level MOTD**  
To change your gun do /gunplus gun [gun name]  
To change join game do /gunplus join  
more in /help gunplus  

You can also add your own guns
Add name of gun to gunNames
Go to ```createBullet``` function and make a new ```else if``` branch with ```pl.Extras.GetString("AMMOTYPE") == [index in gunNames]```
Example: ```else if (pl.Extras.GetString("AMMOTYPE") == gunNames[7]) {```
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

*else if (pl.Extras.GetString("AMMOTYPE") == gunNames[7]) {  
&emsp;pl.Extras["FIRERATE"] = 100;  
&emsp;output.block = Block.FromRaw((ushort)767);  
&emsp;output.damage = 100;  
&emsp;output.killMessage = "8=D ";  
&emsp;output.xSpread = 0;  
&emsp;output.ySpread = 0;  
&emsp;output.reach = 10000;  
&emsp;output.numBullets = 10;  
}*  
Reload the plugin now your gun should be in game

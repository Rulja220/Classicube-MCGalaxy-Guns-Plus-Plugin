using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MCGalaxy;
using MCGalaxy.Maths;
using MCGalaxy.Tasks;
using MCGalaxy.Blocks;
using MCGalaxy.Events.ServerEvents;
using MCGalaxy.Events.PlayerEvents;
using BlockID = System.UInt16;


namespace GunsPlus {
	public sealed class GunsPlus : Plugin {
		public override string name { get { return "Guns Plus v1.2"; } }
		public override string MCGalaxy_Version { get { return "1.9.4.8"; } }
		public override string creator { get { return "Rulja1234 (Origian gun code by UnknownShadow200, helped Goodly)"; } }
		
		public Random rnd = new Random();
		public List<string> killMessages = new List<string> {" ", " ", " "};
		public int[] armsraceOrder = new int[] {6,6,4,4,3,3,5,5,2,2,1,1,0};	//The Values is the index of the gunNames
		public string[] gunNames = new string[] {"KNIFE","PISTOL","SHOTGUN","SMG","ASSAULTRIFLE","SNIPERRIFLE","MACHINEGUN"}; //If you are adding a gun, add it at the back
		
		public override void Load(bool startup) {
			OnPlayerCommandEvent.Register(HandleOnPlayerCommand, Priority.Low);
			OnPlayerClickEvent.Register(HandlePlayerClick, Priority.High);
			OnJoinedLevelEvent.Register(HandleOnJoinedLevel, Priority.Low);
			OnPlayerDisconnectEvent.Register(HandlePlayerDisconnect, Priority.High);
			Command.Register(new CmdChange());
			Chat.MessageGlobal("&aGuns Plus plugin succesfully loaded!");
		}
		
		public override void Unload(bool shutdown) {
			Command.Unregister(Command.Find("gunsplus"));
			OnPlayerCommandEvent.Unregister(HandleOnPlayerCommand);
			OnPlayerClickEvent.Unregister(HandlePlayerClick);
			OnJoinedLevelEvent.Unregister(HandleOnJoinedLevel);
			OnPlayerDisconnectEvent.Unregister(HandlePlayerDisconnect);
			Chat.MessageGlobal("&aGuns Plus plugin succesfully unloaded!");
		}
		
		public class CmdChange : Command {
			public override string name { get { return "gunsplus"; } }
			public override string shortcut { get { return "gp"; }}
			public override string type { get { return "fun"; } }
			public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
			
			public GunsPlus GunsPlus = new GunsPlus();
			
			public override void Use(Player pl, string arg) {
				if (pl.GetMotd().Contains("+gunsplus")) {
					arg = arg.ToLower();
					string[] args = arg.Split(" ");
					string folder = Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "plugins/GunsPlus/"+pl.level.name+"&.txt");
					
					//Changing the gun that you are holding
					if (args.Length == 2 && args[0] == "gun" && GunsPlus.checkIn(args[1].ToUpper(), GunsPlus.gunNames) && pl.level.Extras.GetString("GAMEMODE") != "armsrace") {
						pl.Extras["AMMOTYPE"] = args[1].ToUpper(); 
						Cooldown.StartCooldown(pl.truename+"shoot", 1);
						pl.Message("Sucessfully changed the weapon to "+args[1]);
					}
					//Joining a game
					else if (args.Length == 1 && args[0] == "join") {
						string[] file = File.ReadAllText(folder).Split("\n");
						pl.level.Extras["GAMEMODE"] = file[3];
						if (pl.level.Extras.GetInt("BLUETEAM") > pl.level.Extras.GetInt("REDTEAM") && (pl.level.Extras.GetString("GAMEMODE") != "classic" && pl.level.Extras.GetString("GAMEMODE") != "deathmatch")) GunsPlus.game.teamChange(pl, "RED");
						else if (pl.level.Extras.GetString("GAMEMODE") != "classic" && pl.level.Extras.GetString("GAMEMODE") != "deathmatch") GunsPlus.game.teamChange(pl, "BLUE");
						else {GunsPlus.game.teamChange(pl, "GRAY");}
						if (pl.level.Extras.GetString("RUNNING") == "true") GunsPlus.game.joinGame(pl);
					}
					//Leaving a game
					else if (args.Length == 1 && args[0] == "leave") {
						GunsPlus.game.leaveGame(pl, pl.level);
						GunsPlus.game.teamChange(pl, "SPEC");
					}
					//Changing the spawn of a team
					else if (args.Length == 5 && (LevelInfo.IsRealmOwner(pl.truename, pl.level.name) || pl.Rank >= LevelPermission.Admin) && args[0] == "spawn" && (args[1] == "blue" || args[1] == "red" || args[1] == "gray") && int.TryParse(args[2], out _) && int.TryParse(args[3], out _) && int.TryParse(args[4], out _)){ 
						if (pl.level.GetBlock(Convert.ToUInt16(args[2]), Convert.ToUInt16(args[3]), Convert.ToUInt16(args[4])) == 0) {
							int line = 1;
							if (args[1] == "red") line = 2;
							else if (args[1] == "gray") line = 3;
							lineChanger(args[2]+" "+args[3]+" "+args[4] ,folder ,line);
							pl.Message("&aSucessfully changed the spawn of team "+args[1]);
						}
						else {pl.Message("&cSorry, but that is an invalid spot for a team spawn!");}
					}
					//Changing the gamemode
					else if (args.Length == 2 && (LevelInfo.IsRealmOwner(pl.truename, pl.level.name) || pl.Rank >= LevelPermission.Admin) && args[0] == "gamemode" && (args[1] == "classic" || args[1] == "teamdeathmatch" || args[1] == "deathmatch" || args[1] == "armsrace") && pl.level.Extras.GetString("RUNNING") == "false") {												
					Player[] players = PlayerInfo.Online.Items;
						foreach (Player pls in players) {
							if (pl.level != pls.level) continue;
							GunsPlus.game.teamChange(pls, "SPEC");
						}
						pl.level.Extras["BLUETEAM"] = 0;
						pl.level.Extras["REDTEAM"] = 0;
						pl.level.Extras["GRAYTEAM"] = 0;
						lineChanger(args[1] ,folder ,4);
						pl.level.Extras["GAMEMODE"] = args[1];
						pl.level.Message("&aSucessfully changed the gamemode to "+args[1]);
					}
					//Starting the game
					else if (args.Length == 1 && args[0] == "start" && (LevelInfo.IsRealmOwner(pl.truename, pl.level.name) || pl.Rank >= LevelPermission.Admin)) {
						string[] file = File.ReadAllText(folder).Split("\n");
						if (file[0] != "NOT SET" && file[1] != "NOT SET" && file[2] != "NOT SET") {
							pl.level.Extras["BLUESPAWN"] = file[0];
							pl.level.Extras["REDSPAWN"] = file[1];
							pl.level.Extras["GRAYSPAWN"] = file[2];
							pl.level.Extras["GAMEMODE"] = file[3];
							if (pl.level.Extras.GetString("RUNNING") == "false" || pl.level.Extras.GetString("RUNNING") == null) {
								if (pl.level.Extras.GetInt("BLUETEAM") + pl.level.Extras.GetInt("REDTEAM") + pl.level.Extras.GetInt("GRAYTEAM") > 1) GunsPlus.startGame(pl);
								else {pl.Message("&cYou need more players to start!");}
							}
							else {pl.Message("&cThe game is alredy running!");}
						}
						else {
							if (file[0] == "NOT SET") {pl.Message("&cYou didn't set the spawn of the blue team!");}
							else if (file[1] == "NOT SET") {pl.Message("&cYou didn't set the spawn of the red team!");}
							else if (file[2] == "NOT SET") {pl.Message("&cYou didn't set the spawn of the gray team!");}
						}
					}
					//Ending the game
					else if (args.Length == 1 && args[0] == "end" && (LevelInfo.IsRealmOwner(pl.truename, pl.level.name) || pl.Rank >= LevelPermission.Admin)) {
						if (pl.level.Extras.GetString("RUNNING") == "true") {GunsPlus.endGame(pl, "No one");}
						else {pl.Message("&cThere are no running games on this map!");}
					}
					//Error if the command is wrong
					else {pl.Message("%cSorry that is invalid input or you can't do that now! Please do /help gunsplus");}
				}
				//Error if level don't have +gunsplus in it's motd
				else {pl.Message("%cSorry this level doesn't use Guns Plus! To activate it add +gunsplus to the level MOTD.");}
			}

			public override void Help(Player p) {
				p.Message("%T/gunsplus join &e- Join a game of Gun Plus");
				p.Message("%T/gunsplus gamemode <gamemode> (Staff or Level Owner only)");
				p.Message("&eGamemodes: &fclassic/teamdeathmatch/deathmatch/armsrace");
				p.Message("%T/gunsplus spawn <team> <x y z> (Staff or Level Owner only)");
				p.Message("&eTeams: &fBlue/Red/Gray");
				p.Message("%T/gunsplus start &e- Start the game (Staff or Level Owner only)");							
				p.Message("%T/gunsplus gun <gun>");
				string gunsPrint = "";
				for (int i = 0; i < GunsPlus.gunNames.Length-1; i++) {gunsPrint = gunsPrint + GunsPlus.gunNames[i] + "/";} 
				gunsPrint = gunsPrint + GunsPlus.gunNames[GunsPlus.gunNames.Length-1];
				p.Message("&eWeapons: &f"+gunsPrint);			
				p.Message("%T/gunsplus end &e- End the game (Staff or Level Owner only)");
			}
		}
		
		void HandleOnPlayerCommand(Player pl, string cmd, string args, CommandData data) {
			if ((cmd == "Overseer" && args.StartsWith("map motd")) || (cmd == "map" && args.StartsWith("motd")) && args.Contains(" +gunsplus")) {
				string folder = Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "plugins/GunsPlus");
				if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
				string file = Path.Combine(folder, pl.level.name+"&.txt");
				if (!File.Exists(file)) {
					FileStream fs = File.Create(file);
					string[] write = {"NOT SET", "NOT SET", "NOT SET", "classic"};
					File.WriteAllLines(file, write);
				}
				Player[] players = PlayerInfo.Online.Items;
				foreach (Player pls in players) {
					if (pl.level != pls.level) continue;
					playerLoad(pls);
				}
				pl.Message("&aYou have successfully added Guns Plus to this level.");
			}	
		}
		
		void HandleOnJoinedLevel(Player pl, Level prevLevel, Level nowlevel, ref bool announce) {
			if (prevLevel.Config.MOTD.Contains("+gunsplus") == true) game.leaveGame(pl, prevLevel);
			if (nowlevel.Config.MOTD.Contains("+gunsplus")) {
				playerLoad(pl);
				pl.Message("&eThis map uses Guns Plus, do /help gunsplus for more info");
			}
		}
		
		void playerLoad(Player pl) {
			pl.Extras["HEALTH"] = 100;
			pl.Extras["KILLS"] = 0;
			pl.Extras["DEATHS"] = 0;
			pl.Extras["KILLSTREAK"] = 0;
			pl.Extras["AMMOTYPE"] = "PISTOL";
			pl.Extras["FIRERATE"] = 1;
			game.teamChange(pl, "SPEC");
			if (pl.level.Extras.GetString("RUNNING") == null) {
				string[] file = File.ReadAllText(Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "plugins/GunsPlus/"+pl.level.name+"&.txt")).Split("\n");
				pl.level.Extras["RUNNING"] = "false";
				pl.level.Extras["GAMEMODE"] = file[3];
				pl.level.Extras["BLUETEAM"] = 0;
				pl.level.Extras["REDTEAM"] = 0;
				pl.level.Extras["GRAYTEAM"] = 0;
			}
			pl.Message("Current gamemode is "+pl.level.Extras.GetString("GAMEMODE"));
			pl.SendCpeMessage(CpeMessageType.BottomRight3, " ");
			pl.SendCpeMessage(CpeMessageType.BottomRight2, " ");
			pl.SendCpeMessage(CpeMessageType.BottomRight1, " ");
			pl.SendCpeMessage(CpeMessageType.Status3, " ");
			pl.SendCpeMessage(CpeMessageType.Status2, " ");
			pl.SendCpeMessage(CpeMessageType.Status1, " ");
		}
		
        void HandlePlayerDisconnect(Player pl, string reason) {
			if (pl.level.Config.MOTD.Contains("+gunsplus") == true) game.leaveGame(pl, pl.level);
		}
		
		
		public class game {
			public static GunsPlus GunsPlus = new GunsPlus();
			
			public static void joinGame (Player pl) {
				if (pl.level.Extras.GetString("RUNNING") == "false") {pl.Extras["TEAM"] = pl.Extras.GetString("PICKEDTEAM");}
				Command.Find("tp").Use(pl, pl.level.Extras.GetString(pl.Extras.GetString("TEAM")+"SPAWN"));
				if (pl.level.Extras.GetString("GAMEMODE") == "classic") {joinClassic(pl);}
				else if (pl.level.Extras.GetString("GAMEMODE") == "teamdeathmatch") {joinTeamDeathMatch(pl);}
				else if (pl.level.Extras.GetString("GAMEMODE") == "deathmatch") {joinDeathMatch(pl);}
				else if (pl.level.Extras.GetString("GAMEMODE") == "armsrace") {joinArmsRace(pl);}
				Cooldown.StartCooldown(pl.truename+"inv", 10000);
			}
			
			public static void leaveGame (Player pl, Level level) {
				Command.Find("model").Use(pl, "humanoid");
				pl.Extras["KILLS"] = 0;
				pl.Extras["DEATHS"] = 0;
				pl.Extras["KILLSTREAK"] = 0;
				pl.Extras["TEAM"] = pl.Extras.GetString("PICKEDTEAM");
				level.Extras[pl.Extras.GetString("TEAM")+"TEAM"] = level.Extras.GetInt(pl.Extras.GetString("TEAM")+"TEAM") - 1;
				pl.Extras["PICKEDTEAM"] = "None";
				pl.Extras["TEAM"] = "None";
				pl.Message("&aYou have left the game of Gun Plus");
				if (level.Config.MOTD.Contains("+gunsplus") == false) {level.Message(""+pl.ColoredName+"&e has left a game of Gun Plus");}
			}
			
			public static void teamChange(Player pl, string team) {
				if (pl.Extras.GetString("TEAM") == "BLUE" || pl.Extras.GetString("TEAM") == "RED" || pl.Extras.GetString("TEAM") == "GRAY" || pl.Extras.GetString("PICKEDTEAM") == "BLUE" || pl.Extras.GetString("PICKEDTEAM") == "RED" || pl.Extras.GetString("PICKEDTEAM") == "GRAY") {leaveGame(pl, pl.level);}
				if (pl.level.Extras.GetString("RUNNING") == "false") {
					pl.Extras["PICKEDTEAM"] = team;
					if (team != "SPEC") {pl.level.Message(pl.ColoredName+"&e will join the "+team+" team.");}
				}
				else {
					pl.Extras["TEAM"] = team;
					pl.level.Message(pl.ColoredName+"&e joined the "+team+" team.");
				}
				if (team != "SPEC") pl.level.Extras[team+"TEAM"] = pl.level.Extras.GetInt(team+"TEAM") + 1;
				if (team == "SPEC") Command.Find("model").Use(pl, "0");
				else{Command.Find("model").Use(pl, "humanoid");}
			}
			
			public static void joinClassic (Player pl) {
				teamChange(pl, "GRAY");
				pl.Message("&eYou joined a game of Classic Gun Battle! Objective is to kill other players.");
			}
			
			public static void joinTeamDeathMatch (Player pl) {
				pl.Message("&eYou joined a game of Team Deathmatch! Objective is to kill players in the opposite team (There are no respawns).");
			}
			
			public static void joinDeathMatch (Player pl) {
				teamChange(pl, "GRAY");
				pl.Message("&eYou joined a game of Deathmatch! Objective is to kill other players (There are no respawns).");
			}
			
			public static void joinArmsRace (Player pl) {
				pl.Extras["AMMOTYPE"] = GunsPlus.gunNames[GunsPlus.armsraceOrder[0]];
                pl.Message("Your gun changed to "+GunsPlus.gunNames[GunsPlus.armsraceOrder[0]]);
				pl.Message("&eYou joined a game of Arms Race! When you kill the opposite team players your gun changes.");
		    }
		}
		
		void startGame(Player pl) {
			pl.level.Extras["RUNNING"] = "true";
			Player[] players = PlayerInfo.Online.Items;
			foreach (Player pls in players) {
				if (pl.level != pls.level || pls.Extras.GetString("PICKEDTEAM") == "SPEC" || pls.Extras.GetString("PICKEDTEAM") == "None") continue;
				game.joinGame(pls);
				pls.SendCpeMessage(CpeMessageType.Announcement,"%aThe game has started");
			}
		}
		
		void endGame(Player pl, string winner) {
			pl.level.Extras["BLUETEAM"] = 0;
			pl.level.Extras["REDTEAM"] = 0;
			pl.level.Extras["GRAYTEAM"] = 0;
			Player[] players = PlayerInfo.Online.Items;
			killMessages = new List<string> {" ", " ", " "};
			foreach (Player pls in players) {
				if (pl.level != pls.level) continue;
				playerLoad(pls);
				pls.SendCpeMessage(CpeMessageType.Announcement, "%a"+winner+" %ahas won!");
			}
			pl.level.Extras["RUNNING"] = "false";
		}
		
		void HandlePlayerClick(Player pl,MouseButton button, MouseAction action,ushort yaw, ushort pitch,byte entity, ushort x, ushort y, ushort z,TargetBlockFace face){
			if (button == MouseButton.Left && action == MouseAction.Pressed && Cooldown.IsCooledDown(pl, pl.truename+"shoot") && pl.Extras.GetString("TEAM") != "SPEC" && pl.GetMotd().Contains("+gunsplus") && pl.level.Extras.GetString("RUNNING") == "true") {
				BulletData bullet = createBullet(pl);
				int ry;
				int rx;
				Vec3F32 dir;
				for (int i = 0; i < bullet.numBullets;i++) {
					ry = rnd.Next(-(bullet.ySpread), bullet.ySpread);
					rx = rnd.Next(-(bullet.xSpread), bullet.xSpread);
					dir = DirUtils.GetDirVector((byte)(pl.Rot.RotY+ry), (byte)(pl.Rot.HeadX+rx));
					shoot(dir, pl, bullet);
				}
				Cooldown.StartCooldown(pl.truename+"shoot", pl.Extras.GetInt("FIRERATE"));
			}
		}
		
        void shoot(Vec3F32 dir, Player pl, BulletData bullet) {
            AmmunitionData args = AmmunitionData.MakeArgs(dir, pl, pl.Extras.GetString("AMMOTYPE"), bullet);
            SchedulerTask task  = new SchedulerTask(GunCallback, args, TimeSpan.Zero, true);
            pl.CriticalTasks.Add(task);
        }

		BulletData createBullet(Player pl) {
			BulletData output = new BulletData();
			if (pl.Extras.GetString("AMMOTYPE") == gunNames[0]) { //Knife
				pl.Extras["FIRERATE"] = 350;
				output.block = Block.FromRaw((ushort)53);
				output.damage = 200;
				output.killMessage = " &6═&8╣═- ";
				output.xSpread = 10;
				output.ySpread = 10;
				output.reach = 1;
				output.numBullets = 5;
			}
			else if (pl.Extras.GetString("AMMOTYPE") == gunNames[1]) { //Pistol
				pl.Extras["FIRERATE"] = 900;
				output.block = Block.FromRaw((ushort)39);
				output.damage = 20;
				output.killMessage = " &8╔═╧ ";
				output.xSpread = 3;
				output.ySpread = 3;
				output.reach = 50;
				output.numBullets = 1;
			}
			else if (pl.Extras.GetString("AMMOTYPE") == gunNames[2]) { //Shotgun
				pl.Extras["FIRERATE"] = 1500;
				output.block = Block.FromRaw((ushort)39);
				output.damage = 5;
				output.killMessage = " &0▄&8╤═&0╤&8╧ ";
				output.xSpread = 15;
				output.ySpread = 15;
				output.reach = 20;
				output.numBullets = 15;
			}
			else if (pl.Extras.GetString("AMMOTYPE") == gunNames[3]) { //SMG
				pl.Extras["FIRERATE"] = 100;
				output.block = Block.FromRaw((ushort)39);
				output.damage = 10;
				output.killMessage = " &8█▌&6╤&8╬- ";
				output.xSpread = 4;
				output.ySpread = 4;
				output.reach = 35;
				output.numBullets = 1;
			}
			else if (pl.Extras.GetString("AMMOTYPE") == gunNames[4]) { //Assault Rifle
				pl.Extras["FIRERATE"] = 350;
				output.block = Block.FromRaw((ushort)39);
				output.damage = 15;
				output.killMessage = " &6⌐&8╤╦&6═&8┴ ";
				output.xSpread = 3;
				output.ySpread = 3;
				output.reach = 75;
				output.numBullets = 1;				
			}
			else if (pl.Extras.GetString("AMMOTYPE") == gunNames[5]) { //Sniper Rifle
				pl.Extras["FIRERATE"] = 1500;
				output.block = Block.FromRaw((ushort)39);
				output.damage = 60;
				output.killMessage = " &0▐&6█▀═&8╣╚&6═&8--- ";
				output.xSpread = 0;
				output.ySpread = 0;
				output.reach = 1000;
				output.numBullets = 1;
			}
			else if (pl.Extras.GetString("AMMOTYPE") == gunNames[6]) { //Machine Gun
				pl.Extras["FIRERATE"] = 300;
				output.block = Block.FromRaw((ushort)39);
				output.damage = 20;
				output.killMessage = " &8─╦═&a█&8╝╧ ";
				output.xSpread = 6;
				output.ySpread = 6;
				output.reach = 45;
				output.numBullets = 1;
			}
			output.reach = output.reach * output.numBullets;
			return output;
		}

        bool OnHitBlock(AmmunitionData args, Vec3U16 pos, BlockID block) {return true;}
		
        void OnHitPlayer(AmmunitionData args, Player pl) {
			pl.Extras["HEALTH"] = pl.Extras.GetInt("HEALTH") - args.bulletargs.damage;
			pl.SendCpeMessage(CpeMessageType.BottomRight2, "You took "+args.bulletargs.damage+" damage");
			if (pl.Extras.GetInt("HEALTH") < 1) {die(args, pl);}
			string color = "&0";
			if (pl.Extras.GetInt("HEALTH") > 75) {color = "&4";}
			else if (75 >= pl.Extras.GetInt("HEALTH") && pl.Extras.GetInt("HEALTH") > 50) {color = "&4";}
			else if (50 >= pl.Extras.GetInt("HEALTH") && pl.Extras.GetInt("HEALTH") > 25) {color = "&c";}
			else if (25 >= pl.Extras.GetInt("HEALTH") && pl.Extras.GetInt("HEALTH") > 10) {color = "&7";}
			else if (10 >= pl.Extras.GetInt("HEALTH") && pl.Extras.GetInt("HEALTH") > 0) {color = "&8";}
			pl.SendCpeMessage(CpeMessageType.BottomRight1, ""+color+""+pl.Extras.GetInt("HEALTH")+" ♥");
			args.hit = true;
        }

		static Player PlayerAt(Player pls, Vec3U16 pos, bool skipSelf) {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players) {
                if (pl.level != pls.level) continue;
                if (pls == pl && skipSelf) continue;
                
                if (Math.Abs(pl.Pos.BlockX - pos.X)    <= 1
                    && Math.Abs(pl.Pos.BlockY - pos.Y) <= 1
                    && Math.Abs(pl.Pos.BlockZ - pos.Z) <= 1)
                {
                    return pl;
                }
            }
            return null;
        }
        
		void die(AmmunitionData args, Player pl) {
			Command.Find("tp").Use(pl, pl.level.Extras.GetString(pl.Extras.GetString("TEAM")+"SPAWN"));
			if (pl.level.Extras.GetString("GAMEMODE") == "deathmatch") {
				pl.level.Extras["GRAYTEAM"] = pl.level.Extras.GetInt("GRAYTEAM") - 1;
				game.teamChange(pl, "SPEC");
				if (pl.level.Extras.GetInt("GRAYTEAM") < 2) {endGame(pl, args.shotBy.ColoredName);}
			}
			else if (pl.level.Extras.GetString("GAMEMODE") == "teamdeathmatch") {
				pl.level.Extras[pl.Extras.GetString("TEAM")+"TEAM"] = pl.level.Extras.GetInt(pl.Extras.GetString("TEAM")+"TEAM") - 1;
				game.teamChange(pl, "SPEC");
				if (pl.level.Extras.GetInt("BLUETEAM") < 0) {endGame(pl, "Red team");}
				else if (pl.level.Extras.GetInt("REDTEAM") < 0) {endGame(pl, "Blue team");}
			}
			else if (pl.level.Extras.GetString("GAMEMODE") == "armsrace") {
				if (armsraceOrder.Length > args.shotBy.Extras.GetInt("KILLS")+1) {
					if (args.shotBy.Extras.GetString("AMMOTYPE") != gunNames[armsraceOrder[args.shotBy.Extras.GetInt("KILLS")+1]]) {args.shotBy.Message("Your gun changed to "+gunNames[armsraceOrder[args.shotBy.Extras.GetInt("KILLS")+1]]);}
					args.shotBy.Extras["AMMOTYPE"] = gunNames[armsraceOrder[args.shotBy.Extras.GetInt("KILLS")+1]];
				}
				else {
					if (args.shotBy.Extras.GetString("TEAM") == "BLUE") {endGame(args.shotBy, "Blue team");}
					else if (args.shotBy.Extras.GetString("TEAM") == "RED") {endGame(args.shotBy, "Red team");}
				}
			}
			pl.Extras["DEATHS"] = pl.Extras.GetInt("DEATHS") + 1;
			args.shotBy.Extras["KILLSTREAK"] = args.shotBy.Extras.GetInt("KILLSTREAK") + 1;
			args.shotBy.Extras["KILLS"] = args.shotBy.Extras.GetInt("KILLS") + 1;

			if (args.shotBy.Extras.GetInt("KILLSTREAK")%5 == 0) {killMessages.Add("&b"+args.shotBy.ColoredName+" &bhas killstreak of "+args.shotBy.Extras.GetInt("KILLSTREAK"));}
			if (pl.Extras.GetInt("KILLSTREAK") > 4) {killMessages.Add("&b"+args.shotBy.ColoredName+" &bhas ended "+pl.ColoredName+" &bof "+pl.Extras.GetInt("KILLSTREAK"));}
			killMessages.Add(args.shotBy.ColoredName +""+ args.bulletargs.killMessage +""+ pl.ColoredName);
			while (killMessages.Count > 3) {killMessages.RemoveAt(0);}
			pl.SendCpeMessage(CpeMessageType.BottomRight2, " ");
			showKills(pl);
			
			pl.Extras["KILLSTREAK"] = 0;
			Cooldown.StartCooldown(pl.truename+"inv", 5000);
			pl.Extras["HEALTH"] = 100;
		}
		
        bool TickMove(AmmunitionData args) {
            if (args.iterations > 2) {
                Vec3U16 pos = args.visible[0];
                args.visible.RemoveAt(0);
                args.shotBy.level.BroadcastRevert(pos.X, pos.Y, pos.Z);
            }
            return true;
        }
        
        bool TickRevert(SchedulerTask task) {
            AmmunitionData args = (AmmunitionData)task.State;
            
            if (args.visible.Count > 0) {
                Vec3U16 pos = args.visible[0];
                args.visible.RemoveAt(0);
                args.shotBy.level.BroadcastRevert(pos.X, pos.Y, pos.Z);
            }
            return args.visible.Count > 0;
        }
		
        void GunCallback(SchedulerTask task) {
            AmmunitionData args = (AmmunitionData)task.State;
            if (args.moving) {
                args.moving = TickGun(args);
            } else {
                task.Repeating = TickRevert(task);
            }
        }
		
        bool TickGun(AmmunitionData args) {
            while (true) {
                Vec3U16 pos = args.PosAt(args.iterations);
                args.iterations++;
				
				args.bulletargs.reach--;	
                BlockID cur = args.shotBy.level.GetBlock(pos.X, pos.Y, pos.Z);
                if (cur == Block.Invalid) return false;
				if (args.bulletargs.reach < -1) return false;
                if (cur != Block.Air && !args.all.Contains(pos) && OnHitBlock(args, pos, cur)) return false;
				
                args.shotBy.level.BroadcastChange(pos.X, pos.Y, pos.Z, args.bulletargs.block);
                args.visible.Add(pos);
                args.all.Add(pos);
                
                Player pl = PlayerAt(args.shotBy, pos, true);
                if (pl != null && args.hit == false && (args.team != pl.Extras.GetString("TEAM") || args.team == "GRAY") && "SPEC" != pl.Extras.GetString("TEAM") && Cooldown.IsCooledDown(pl, pl.truename+"inv")) { OnHitPlayer(args, pl); return false; }
				if (TickMove(args)) return true;
            }
        }
		
		void showKills(Player pl) {
            Player[] players = PlayerInfo.Online.Items;
			foreach (Player pls in players) {
				if (pl.level != pls.level) continue;
				pls.SendCpeMessage(CpeMessageType.Status3, killMessages[2]);
				pls.SendCpeMessage(CpeMessageType.Status2, killMessages[1]);
				pls.SendCpeMessage(CpeMessageType.Status1, killMessages[0]);
			}
		}
		
		static bool checkIn(string stringToCheck ,string[] stringArray ) {
			foreach (string x in stringArray) {
				if (stringToCheck.Contains(x)) {
					return true;
				}
			}
			return false;
		}
		
		static void lineChanger(string newText, string fileName, int line_to_edit) {
			 string[] arrLine = File.ReadAllLines(fileName);
			 arrLine[line_to_edit - 1] = newText;
			 File.WriteAllLines(fileName, arrLine);
		}
	}
	public class BulletData {
		public BlockID block;
		public int damage;
		public string killMessage;
		public int xSpread;
		public int ySpread;
		public int reach;
		public int numBullets;
	}
	
	public class AmmunitionData {
		public bool hit;
		public Player shotBy;
		public string team;
		public string ammoType;
		public BulletData bulletargs;
        public Vec3U16 start;
        public Vec3F32 dir;
        public bool moving = true;

        
        public List<Vec3U16> visible = new List<Vec3U16>();
        public List<Vec3U16> all = new List<Vec3U16>();
        public int iterations;
        
        public Vec3U16 PosAt(int i) {
            Vec3U16 target;
            target.X = (ushort)Math.Round(start.X + (double)(dir.X * i));
            target.Y = (ushort)Math.Round(start.Y + (double)(dir.Y * i));
            target.Z = (ushort)Math.Round(start.Z + (double)(dir.Z * i));
            return target;
        }
		
		public static AmmunitionData MakeArgs(Vec3F32 dir, Player pl, string ammoType, BulletData bullet) {
			AmmunitionData args = new AmmunitionData();
			args.bulletargs = bullet;
			args.ammoType = ammoType;
			args.shotBy = pl;
			args.team = pl.Extras.GetString("TEAM");
			args.hit = false;
			args.start = (Vec3U16)pl.Pos.BlockCoords;
			args.dir = dir;
			args.iterations = 1;
			return args;
		}
	}
	public static class Cooldown {
		static readonly object locker = new object();
		static Dictionary<string, DateTime> cooldowns = new Dictionary<string, DateTime>();
		
		public static void StartCooldown(string name, int milliseconds) {
			TimeSpan coolDown = TimeSpan.FromMilliseconds(milliseconds);
			lock (locker) { cooldowns[name] = DateTime.UtcNow + coolDown; }
		}
		
		public static bool IsCooledDown(string name) {
			return GetCooldown(name).TotalSeconds <= 0;
		}

		public static bool IsCooledDown(Player p, string name, string action = "doing this") {
			TimeSpan coolDown = GetCooldown(name);
			if (coolDown.TotalSeconds > 0) {
				coolDown += new TimeSpan(0, 0, 0, 1, 0);
				return false;
			}
			return true;
		}
		
		public static TimeSpan GetCooldown(string name) {
			DateTime expires;
			lock (locker) { cooldowns.TryGetValue(name, out expires); }
			return expires - DateTime.UtcNow;
		}
	}
}

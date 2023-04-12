using System;
using System.Collections;
using System.Collections.Generic;
using MCGalaxy;
using MCGalaxy.Maths;
using MCGalaxy.Tasks;
using MCGalaxy.Blocks;
using MCGalaxy.Events.ServerEvents;
using MCGalaxy.Events.PlayerEvents;
using BlockID = System.UInt16;


namespace Project.guns {
	public sealed class guns : Plugin {
		public override string name { get { return "Guns Plus"; } }
		public override string MCGalaxy_Version { get { return "1.9.4.7"; } }
		public override string creator { get { return "Rulja1234 (Origian gun code by UnknownShadow200, helped Goodly)"; } }
		
		public List<string> killMessages = new List<string> {" ", " ", " "};  
		
		public override void Load(bool startup) {
			OnPlayerClickEvent.Register(HandlePlayerClick, Priority.High);
			OnJoinedLevelEvent.Register(HandleOnJoinedLevel, Priority.Low);
			Command.Register(new CmdChange());
		}
		
		public override void Unload(bool shutdown) {
			Command.Unregister(Command.Find("gunplus"));
			OnPlayerClickEvent.Unregister(HandlePlayerClick);
			OnJoinedLevelEvent.Unregister(HandleOnJoinedLevel);
		}
		public class CmdChange : Command {
			public override string name { get { return "gunplus"; } }
			public override string shortcut { get { return "gp"; }}
			public override string type { get { return "fun"; } }
			public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
			string[] gunNames = new string[] {"PISTOL","SHOTGUN","SMG","ASSAULTRIFLE","SNIPERRIFLE","MACHINEGUN"};
			
			public override void Use(Player pl, string arg) {
				arg = arg.ToUpper();
				string[] args = arg.Split(" ");
				if (args.Length == 1 && checkIn(args[0], gunNames)) {pl.Extras["AMMOTYPE"] = args[0]; pl.Message("Sucessfully changed the weapon to "+args[0]);}
				else if (args.Length == 1 && args[0] == "BLUE" || args[0] == "RED" || args[0] == "SPEC" || args[0] == "GRAY") {
					pl.Extras["TEAM"] = args[0]; 
					pl.Message("Sucessfully changed the team to "+args[0]);
					if(args[0] == "SPEC") {Command.Find("model").Use(pl, "0");} 
					else{Command.Find("model").Use(pl, "humanoid");}
				}
				else if (args.Length == 5 && args[0] == "SPAWN" && (args[1] == "BLUE" || args[1] == "RED" || args[1] == "GRAY")) {pl.level.Extras[args[1] + "SPAWN"] = args[2]+" "+args[3]+" "+args[4];pl.Message("Sucessfully changed the spawn of team "+args[1]);}
				else {pl.Message("%cSorry that is invalid input! Please do /help gunplus");}
			}

			public override void Help(Player p) {
				p.Message("%T/gunplus <gun/team>");
				string gunsPrint = "";
				for (int i = 0; i < gunNames.Length; i++) {gunsPrint = gunsPrint + gunNames[i];}
				p.Message("&eGuns: &f"+gunsPrint);
				p.Message("&eTeams: &fBLUE/RED/SPEC/GRAY");
				p.Message("&eSPEC can't shoot or take damage you are also invisible");
				p.Message("&eGRAY has no teammates, free for all");
				p.Message("%T/gunplus SPAWN <team> <x y z>");
				p.Message("&eTeams: &fBLUE/RED/SPEC/GRAY");
			}
		}
		void HandleOnJoinedLevel(Player pl, Level prevLevel, Level nowlevel, ref bool announce) {
			if (pl.GetMotd().Contains("+gunsplus")) {
				pl.Extras["HEALTH"] = 100;
				pl.Extras["KILLS"] = 0;
				pl.Extras["DEATHS"] = 0;
				pl.Extras["KILLSTREAK"] = 0;
				pl.Extras["AMMOTYPE"] = "ASSAULTRIFLE";
				pl.Extras["TEAM"] = "BLUE";
				pl.Extras["FIRERATE"] = 0;
				pl.SendCpeMessage(CpeMessageType.BottomRight3, " ");
				pl.SendCpeMessage(CpeMessageType.BottomRight2, " ");
				pl.SendCpeMessage(CpeMessageType.BottomRight1, ""+pl.Extras.GetInt("HEALTH"));
				pl.SendCpeMessage(CpeMessageType.Status3, " ");
				pl.SendCpeMessage(CpeMessageType.Status2, " ");
				pl.SendCpeMessage(CpeMessageType.Status1, " ");
			}
		}
		
		void HandlePlayerClick(Player pl,MouseButton button, MouseAction action,ushort yaw, ushort pitch,byte entity, ushort x, ushort y, ushort z,TargetBlockFace face){
			if (button == MouseButton.Left && action == MouseAction.Pressed && Cooldown.IsCooledDown(pl, pl.name) && pl.Extras.GetString("TEAM") != "SPEC" && pl.GetMotd().Contains("+gunsplus")) {
				BulletData bullet = createBullet(pl);
				int ry;
				int rx;
				Vec3F32 dir;
				Random rnd = new Random();
				for (int i = 0; i < bullet.numBullets;i++) {
					ry = rnd.Next(-(bullet.ySpread), bullet.ySpread);
					rx = rnd.Next(-(bullet.xSpread), bullet.xSpread);
					dir = DirUtils.GetDirVector((byte)(pl.Rot.RotY+ry), (byte)(pl.Rot.HeadX+rx));
					shoot(dir, pl, bullet);
				}
				Cooldown.StartCooldown(pl.truename, pl.Extras.GetInt("FIRERATE"));
			}
		}
		
        void shoot(Vec3F32 dir, Player pl, BulletData bullet) {
            AmmunitionData args = MakeArgs(dir, pl, pl.Extras.GetString("AMMOTYPE"), bullet);
            SchedulerTask task  = new SchedulerTask(GunCallback, args, TimeSpan.Zero, true);
            pl.CriticalTasks.Add(task);

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
		
        AmmunitionData MakeArgs(Vec3F32 dir, Player pl, string ammoType, BulletData bullet) {
            AmmunitionData args = new AmmunitionData();
            args.bulletargs = bullet;
			args.ammoType = ammoType;
            args.shotBy = pl;
			args.team = pl.Extras.GetString("TEAM");
			args.hit = false;
            args.start = (Vec3U16)pl.Pos.BlockCoords;
            args.dir = dir;
            args.iterations = 1;
			args.pl = pl;
            return args;
        }

		BulletData createBullet(Player pl) {
			BulletData output = new BulletData();
			if (pl.Extras.GetString("AMMOTYPE") == "PISTOL") {
				pl.Extras["FIRERATE"] = 900;
				output.block = Block.FromRaw((ushort)39);
				output.damage = 20;
				output.killMessage = " &8╔═╧ ";
				output.xSpread = 3;
				output.ySpread = 3;
				output.numBullets = 1;
			}
			else if (pl.Extras.GetString("AMMOTYPE") == "SHOTGUN") {
				pl.Extras["FIRERATE"] = 1500;
				output.block = Block.FromRaw((ushort)39);
				output.damage = 5;
				output.killMessage = " &0▄&8╤═&0╤&8╧ ";
				output.xSpread = 15;
				output.ySpread = 15;
				output.numBullets = 15;
			}
			else if (pl.Extras.GetString("AMMOTYPE") == "SMG") {
				pl.Extras["FIRERATE"] = 100;
				output.block = Block.FromRaw((ushort)39);
				output.damage = 10;
				output.killMessage = " &8█▌&n╤&8╬- ";
				output.xSpread = 4;
				output.ySpread = 4;
				output.numBullets = 1;
			}
			else if (pl.Extras.GetString("AMMOTYPE") == "ASSAULTRIFLE") {
				pl.Extras["FIRERATE"] = 350;
				output.block = Block.FromRaw((ushort)39);
				output.damage = 15;
				output.killMessage = " &n⌐&8╤╦&n═&8┴ ";
				output.xSpread = 3;
				output.ySpread = 3;
				output.numBullets = 1;
			}
			else if (pl.Extras.GetString("AMMOTYPE") == "SNIPERRIFLE") {
				pl.Extras["FIRERATE"] = 1500;
				output.block = Block.FromRaw((ushort)39);
				output.damage = 60;
				output.killMessage = " &0▐&n█▀═&8╣╚&n═&8--- ";
				output.xSpread = 0;
				output.ySpread = 0;
				output.numBullets = 1;
			}
			else if (pl.Extras.GetString("AMMOTYPE") == "MACHINEGUN") {
				pl.Extras["FIRERATE"] = 300;
				output.block = Block.FromRaw((ushort)39);
				output.damage = 20;
				output.killMessage = " &8─╦═&a█&8╝╧ ";
				output.xSpread = 6;
				output.ySpread = 6;
				output.numBullets = 1;
			}
			return output;
		}

        bool OnHitBlock(AmmunitionData args, Vec3U16 pos, BlockID block) {
            return true;
        }
        
		void die(AmmunitionData args, Player pl) {
			if (pl.level.Extras.GetString(pl.Extras.GetString("TEAM")+"SPAWN") != null) {Command.Find("tp").Use(pl, pl.level.Extras.GetString(pl.Extras.GetString("TEAM")+"SPAWN"));}
			else {Command.Find("tp").Use(pl,"64 32 64");}
			killMessages.Add(args.shotBy.ColoredName +""+ args.bulletargs.killMessage +""+ pl.ColoredName);
			if (killMessages.Count > 3) {killMessages.RemoveAt(0);}
			pl.SendCpeMessage(CpeMessageType.BottomRight2, " ");
			showKills();
			pl.Extras["HEALTH"] = 100;
			args.shotBy.Extras["KILLSTREAK"] = pl.Extras.GetInt("KILLSTREAK") + 1;
			pl.Extras["KILLSTREAK"] = 0;
			pl.Extras["DEATHS"] = pl.Extras.GetInt("DEATHS") + 1;
		}
		
        void OnHitPlayer(AmmunitionData args, Player pl) {
			pl.Extras["HEALTH"] = pl.Extras.GetInt("HEALTH") - args.bulletargs.damage;
			pl.SendCpeMessage(CpeMessageType.BottomRight2, "You took "+args.bulletargs.damage+" dmg");
			if (pl.Extras.GetInt("HEALTH") < 1) {die(args, pl);}
			pl.SendCpeMessage(CpeMessageType.BottomRight1, ""+pl.Extras.GetInt("HEALTH"));
			args.hit = true;
        }
        
        bool TickMove(AmmunitionData args) {
            if (args.iterations > 2) {
                Vec3U16 pos = args.visible[0];
                args.visible.RemoveAt(0);
                args.pl.level.BroadcastRevert(pos.X, pos.Y, pos.Z);
            }
            return true;
        }
        
        bool TickRevert(SchedulerTask task) {
            AmmunitionData args = (AmmunitionData)task.State;
            
            if (args.visible.Count > 0) {
                Vec3U16 pos = args.visible[0];
                args.visible.RemoveAt(0);
                args.pl.level.BroadcastRevert(pos.X, pos.Y, pos.Z);
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

                BlockID cur = args.pl.level.GetBlock(pos.X, pos.Y, pos.Z);
                if (cur == Block.Invalid) return false;
                if (cur != Block.Air && !args.all.Contains(pos) && OnHitBlock(args, pos, cur))
                    return false;

                args.pl.level.BroadcastChange(pos.X, pos.Y, pos.Z, args.bulletargs.block);
                args.visible.Add(pos);
                args.all.Add(pos);
                
                Player pl = PlayerAt(args.pl, pos, true);
                if (pl != null && args.hit == false && (args.team != pl.Extras.GetString("TEAM") || args.team == "GRAY") && "SPEC" != pl.Extras.GetString("TEAM")) { OnHitPlayer(args, pl); return false; }
				if (TickMove(args)) return true;
            }
        }
		
		void showKills() {
            Player[] players = PlayerInfo.Online.Items;
			foreach (Player pls in players) {
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
	}
	public class BulletData {
		public BlockID block;
		public int damage;
		public string killMessage;
		public int xSpread;
		public int ySpread;
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
		public Player pl;
        
        // positions of all currently visible "trailing" blocks
        public List<Vec3U16> visible = new List<Vec3U16>();
        // position of all blocks this ammunition has touched/gone through
        public List<Vec3U16> all = new List<Vec3U16>();
        public int iterations;
        
        public Vec3U16 PosAt(int i) {
            Vec3U16 target;
            target.X = (ushort)Math.Round(start.X + (double)(dir.X * i));
            target.Y = (ushort)Math.Round(start.Y + (double)(dir.Y * i));
            target.Z = (ushort)Math.Round(start.Z + (double)(dir.Z * i));
            return target;
        }
	}
	public static class Cooldown {
		static readonly object locker = new object();
		static Dictionary<string, DateTime> cooldowns = new Dictionary<string, DateTime>();
		
		//Sample: Cooldown.StartCooldown(p.name, 1000); //start a cooldown for this player for 1 second 
		public static void StartCooldown(string name, int milliseconds) {
			TimeSpan coolDown = TimeSpan.FromMilliseconds(milliseconds);
			lock (locker) { cooldowns[name] = DateTime.UtcNow + coolDown; }
		}
		//Sample: if (!Cooldown.IsCooledDown(p.name)) { return; } //silently quit if cooldown isn't over
		public static bool IsCooledDown(string name) {
			return GetCooldown(name).TotalSeconds <= 0;
		}
		//Sample: if (!Cooldown.IsCooledDown(p, p.name, "firing your weapon")) { return; } //quit if cooldown isn't over and tell player "You must wait x seconds before firing your weapon again."
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
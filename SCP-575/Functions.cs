﻿using Smod2;
using Smod2.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MEC;
using System.Threading;

namespace SCP575
{
	public class Functions
	{
		public static Functions singleton;
		public SCP575 SCP575;
		public Functions(SCP575 plugin)
		{
			this.SCP575 = plugin;
			Functions.singleton = this;
		}

		public void RunBlackout()
		{
			SCP575.Debug("Blackout Function has started");
			if ((SCP575.timer && SCP575.timed_lcz) || (SCP575.toggle && SCP575.toggle_lcz))
			{
				foreach (Room room in SCP575.BlackoutRoom)
				{
					room.FlickerLights();
				}
			}
			Generator079.generators[0].CallRpcOvercharge();

			if ((SCP575.timer && SCP575.keter) || (SCP575.toggle && SCP575.toggleketer))
			{
				(new Thread(() => Keter())).Start();
			}
		}
		public IEnumerator<float> ToggledBlackout(float delay)
		{
			yield return Timing.WaitForSeconds(delay);
			while (SCP575.toggle)
			{
				(new Thread(() => RunBlackout())).Start();
				yield return Timing.WaitForSeconds(8f);
			}
		}
		public IEnumerator<float> TimedBlackout(float delay)
		{
			SCP575.Debug("Being Delayed");
			yield return Timing.WaitForSeconds(delay);
			while (SCP575.Timed)
			{
				SCP575.Debug("Announcing");
				if (SCP575.announce && SCP575.timed_lcz && SCP575.Timed)
				{
					PlayerManager.localPlayer.GetComponent<MTFRespawn>().CallRpcPlayCustomAnnouncement("FACILITY POWER SYSTEM FAILURE IN 3 . 2 . 1 .", false);
				}
				else
				{
					PlayerManager.localPlayer.GetComponent<MTFRespawn>().CallRpcPlayCustomAnnouncement("HEAVY CONTAINMENT POWER SYSTEM FAILURE IN 3 . 2 . 1 .", false);
				}
				yield return Timing.WaitForSeconds(8.7f);
				float blackout_dur;
				if (SCP575.random_events)
				{
					blackout_dur = GetRandom(SCP575.random_dur_min, SCP575.random_dur_max);
				}
				else
				{
					blackout_dur = SCP575.durTime;
				} 
				SCP575.Debug("Flipping Bools1");
				SCP575.timer = true;
				SCP575.triggerkill = true;
				SCP575.Debug(SCP575.timer.ToString() + SCP575.triggerkill.ToString());
				do
				{
					SCP575.Debug("Running Blackout");
					(new Thread(() => RunBlackout())).Start();
					yield return Timing.WaitForSeconds(8f);
				} while ((blackout_dur -= 11) > 0);
				SCP575.Debug("Announcing Disabled.");
				if (SCP575.announce && SCP575.timed_lcz && SCP575.Timed)
				{
					PlayerManager.localPlayer.GetComponent<MTFRespawn>().CallRpcPlayCustomAnnouncement("FACILITY POWER SYSTEM NOW OPERATIONAL", false);
				}
				else
				{
					PlayerManager.localPlayer.GetComponent<MTFRespawn>().CallRpcPlayCustomAnnouncement("HEAVY CONTAINMENT POWER SYSTEM NOW OPERATIONAL", false);
				}
				yield return Timing.WaitForSeconds(8.7f);
				SCP575.Debug("Flipping bools2");
				SCP575.timer = false;
				SCP575.triggerkill = false;
				SCP575.Debug("Timer: " + SCP575.timer);
				SCP575.Debug("Waiting to re-execute..");
				if (SCP575.random_events)
				{
					float rand = GetRandom(SCP575.random_min, SCP575.random_max);
					yield return rand;
				}
				else
				{
					yield return Timing.WaitForSeconds(SCP575.waitTime);
				}
			}
		}
		public int GetRandom(int min, int max)
		{
			lock(SCP575.gen)
			{
				return SCP575.gen.Next(min, max);
			}
		}
		public void Keter()
		{
			SCP575.Debug("Keter function started.");
			List<Player> players = SCP575.Server.GetPlayers();
			List<String> keterlist = new List<String>();
			int limit = 0;
			for (int i = 0; i < SCP575.keterkill_num; i++)
			{
				if (!SCP575.keterkill || limit > 50 || players.Count == 0) break;
				Player ply = players[UnityEngine.Random.Range(0, players.Count)];
				if (ply.TeamRole.Team != Smod2.API.Team.SPECTATOR && ply.TeamRole.Team != Smod2.API.Team.SCP && IsInDangerZone(ply) && !HasFlashlight(ply))
				{
					players.RemoveAll(p => p.PlayerId == ply.PlayerId);
					keterlist.Add(ply.Name);
				}
				else
				{
					i--;
					limit++;
				}
			}

			foreach (Player player in SCP575.Server.GetPlayers())
			{
				if (player.TeamRole.Team == Smod2.API.Team.SPECTATOR || player.TeamRole.Team == Smod2.API.Team.SCP) continue;
				if (HasFlashlight(player)) continue;
				if (!IsInDangerZone(player)) continue;
			
					if (keterlist.Any(p => player.Name == p) && SCP575.keterkill)
					{
						player.Kill();
						SCP575.Debug("Killing " + player.Name + ".");
						keterlist.Remove(player.Name);
						player.PersonalClearBroadcasts();
						player.PersonalBroadcast(15, "You were killed by SCP-575. Having a flashlight out while in an area affected by a blackout will save you from this!", false);
					}
					else if(!SCP575.keterkill)
					{
						player.Damage(SCP575.KeterDamage);
						SCP575.Debug("Damaging " + player.Name + ".");
						player.PersonalBroadcast(5, "You were damaged by SCP-575!", false);
					}
				SCP575.triggerkill = false;
			}
		}

		public bool HasFlashlight(Player player) =>
			(player.GetGameObject() as GameObject).GetComponent<WeaponManager>().NetworksyncFlash || player.GetCurrentItem().ItemType == ItemType.FLASHLIGHT;

		public bool IsInDangerZone(Player player)
		{
			Vector loc = player.GetPosition();
			foreach (Room room in SCP575.Server.Map.Get079InteractionRooms(Scp079InteractionType.CAMERA).Where(p => Vector.Distance(loc, p.Position) <= 10f))
			{
				if (room.ZoneType == ZoneType.HCZ || (SCP575.timer && SCP575.timed_lcz && room.ZoneType == ZoneType.LCZ) || (SCP575.toggle && SCP575.toggle_lcz && room.ZoneType == ZoneType.LCZ))
					return true;
			}
			return false;
		}
		public void Get079Rooms()
		{
			foreach (Room room in PluginManager.Manager.Server.Map.Get079InteractionRooms(Scp079InteractionType.CAMERA))
			{
				if (room.ZoneType == ZoneType.LCZ)
				{
					SCP575.BlackoutRoom.Add(room);
				}
			}
		}
		public void ToggleBlackout()
		{
			SCP575.toggle = !SCP575.toggle;
			if (SCP575.Timed)
			{
				SCP575.timed_override = true;
				SCP575.Timed = false;
			}
			else if (SCP575.timed_override)
			{
				SCP575.timed_override = false;
				SCP575.Timed = true;
			}
			if (SCP575.toggle)
			{
				if (SCP575.announce)
				{
					if (!SCP575.toggle_lcz)
					{
						PlayerManager.localPlayer.GetComponent<MTFRespawn>().CallRpcPlayCustomAnnouncement("HEAVY CONTAINMENT POWER SYSTEM FAILURE IN 3. . 2. . 1. . ", false);
					}
					else if (SCP575.toggle_lcz)
					{
						PlayerManager.localPlayer.GetComponent<MTFRespawn>().CallRpcPlayCustomAnnouncement("FACILITY POWER SYSTEM FAILURE IN 3. . 2. . 1. . ", false);
					}
				}
				EventsHandler.coroutines.Add(Timing.RunCoroutine(ToggledBlackout(8.7f)));
			}
		}

		public void EnableBlackouts()
		{
			SCP575.Timed = true;
		}
		public void DisableBlackouts()
		{
			SCP575.Timed = false;
		}
		public void EnableAnnounce()
		{
			SCP575.announce = true;
		}
		public void DisableAnnounce()
		{
			SCP575.announce = false;
		}
	}
}

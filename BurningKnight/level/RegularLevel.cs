using System.Collections.Generic;
using BurningKnight.entity.room;
using BurningKnight.level.biome;
using BurningKnight.level.builders;
using BurningKnight.level.rooms;
using BurningKnight.level.rooms.challenge;
using BurningKnight.level.rooms.darkmarket;
using BurningKnight.level.rooms.entrance;
using BurningKnight.level.rooms.payed;
using BurningKnight.level.rooms.preboss;
using BurningKnight.level.rooms.scourged;
using BurningKnight.level.rooms.secret;
using BurningKnight.level.rooms.shop.sub;
using BurningKnight.level.rooms.special;
using BurningKnight.level.rooms.spiked;
using BurningKnight.level.rooms.trap;
using BurningKnight.level.tile;
using BurningKnight.level.variant;
using BurningKnight.save;
using BurningKnight.state;
using Lens.util;
using Lens.util.math;
using MonoGame.Extended.Collections;

namespace BurningKnight.level {
	public class RegularLevel : Level {
		private List<RoomDef> rooms;

		public RegularLevel(BiomeInfo biome) : base(biome) {
			
		}

		public RegularLevel() : base(null) {
			
		}

		public override int GetPadding() {
			return 10;
		}

		public void Generate() {
			Run.Level = this;
			rooms = null;
			ItemsToSpawn = new List<string>();
			Variant = VariantRegistry.Generate(LevelSave.BiomeGenerated.Id);

			if (Variant == null) {
				Variant = new RegularLevelVariant();
			}

			if (Run.Depth > 0) {
				if (GlobalSave.IsTrue("saved_npc")) {
					for (var i = 0; i < Rnd.Int(1, Run.Depth); i++) {
						ItemsToSpawn.Add("bk:emerald");
					}
				}
			}
			
			Build();
			Paint();

			if (rooms == null) {
				return;
			}

			TileUp();
			CreateBody();
			CreateDestroyableBody();
			LoadPassable();
			
			Log.Info("Done!");
		}

		protected void Paint() {
			Log.Info("Painting...");
			var p = GetPainter();
			LevelSave.BiomeGenerated.ModifyPainter(this, p);
			p.Paint(this, rooms);
		}

		protected void Build() {
			var Builder = GetBuilder();
			var Rooms = CreateRooms();

			Rooms = (List<RoomDef>) Rooms.Shuffle(Rnd.Generator);

			var Attempt = 0;

			do {
				Log.Info($"Generating (attempt {Attempt}, seed {Rnd.Seed})...");

				foreach (var Room in Rooms) {
					Room.Connected.Clear();
					Room.Neighbours.Clear();
				} 

				var Rm = new List<RoomDef>();
				Rm.AddRange(Rooms);
				rooms = Builder.Build(Rm);

				var a = rooms == null;
				var b = false;
				
				if (!a) {
					foreach (var r in Rm) {
						if (r.IsEmpty()) {
							Log.Error("Found an empty room!");
							b = true;
							break;
						}
					}
				}
				
				if (a || b) {
					rooms = null;
				
					Log.Error($"Failed! {Builder.GetType().Name}");
					Area.Destroy();
					Area.Add(Run.Level);
					LevelSave.FailedAttempts++;
					Builder = GetBuilder();

					if (Attempt >= 10) {
						Log.Error("Too many attempts to generate a level! Trying a different room set!");
						Attempt = 0;
						Rooms = CreateRooms();
						Rooms = (List<RoomDef>) Rooms.Shuffle(Rnd.Generator);
					}

					Attempt++;
				}
			} while (rooms == null);
		}

		private bool IsFinal() {
			return Run.Depth == Run.ContentEndDepth;
		}

		protected virtual List<RoomDef> CreateRooms() {
			var rooms = new List<RoomDef>();
			var biome = LevelSave.BiomeGenerated;
			var final = IsFinal();
			var rush = Run.Type == RunType.BossRush;
			var first = Run.Depth % 2 == 1;

			if (final) {
				Log.Info("Prepare for the final!");
			}
			
			Log.Info($"Generating a level for {biome.Id} biome");
			
			rooms.Add(new EntranceRoom());

			var regular = rush || final ? 0 : biome.GetNumRegularRooms();
			var special = rush || final ? 0 : biome.GetNumSpecialRooms();
			var trap = rush || final ? 0 : biome.GetNumTrapRooms();
			var connection = rush || final ? 1 : GetNumConnectionRooms();
			var secret = rush || final ? 0 : biome.GetNumSecretRooms();
			
			Log.Info($"Creating r{regular} sp{special} c{connection} sc{secret} t{trap} rooms");

			for (var I = 0; I < regular; I++) {
				rooms.Add(RoomRegistry.Generate(RoomType.Regular, biome));
			}

			for (var i = 0; i < trap; i++) {
				rooms.Add(RoomRegistry.Generate(RoomType.Trap, biome));
			}

			for (var I = 0; I < special; I++) {
				var room = RoomRegistry.Generate(RoomType.Special, biome);
				if (room != null) rooms.Add(room);
			}
			
			for (var I = 0; I < connection; I++) {
				rooms.Add(RoomRegistry.Generate(RoomType.Connection, biome));
			}

			if (!rush && !final && Run.Type != RunType.Challenge) {
				rooms.Add(RoomRegistry.Generate(RoomType.Treasure, biome));

				if (!first) {
					rooms.Add(RoomRegistry.Generate(RoomType.Shop, biome));
				}
			}

			if (rush) {
				rooms.Add(RoomRegistry.Generate(RoomType.Boss, biome));
				rooms.Add(new PrebossRoom());	
			} else if (first) {
				rooms.Add(new ExitRoom());				
			} else {
				rooms.Add(RoomRegistry.Generate(RoomType.Boss, biome));
				rooms.Add(new PrebossRoom());	
				rooms.Add(RoomRegistry.Generate(RoomType.Granny, biome));
				rooms.Add(RoomRegistry.Generate(RoomType.OldMan, biome));
			}

			if (!rush) {
				if (Rnd.Chance(95)) {
					if (Rnd.Chance(2 + Run.Scourge * 5)) {
						rooms.Add(new ScourgedRoom());
					} else {
						if (Rnd.Chance()) {
							rooms.Add(new ChallengeRoom());
						} else {
							rooms.Add(new SpikedRoom());
						}
					}
				}

				var addDarkMarket = (Run.Depth > 2 && Rnd.Chance(10) && GameSave.IsFalse("saw_blackmarket"));

				if (addDarkMarket) {
					rooms.Add(new DarkMarketEntranceRoom());
					rooms.Add(new DarkMarketRoom());
				}

				if (!addDarkMarket && Rnd.Chance(1)) {
					secret--;
					rooms.Add(new SecretDarkMarketEntranceRoom());
					rooms.Add(new DarkMarketRoom());
				}

				for (var I = 0; I < secret; I++) {
					rooms.Add(RoomRegistry.Generate(RoomType.Secret, biome));
				}

				if (Rnd.Chance()) {
					var c = Rnd.Int(0, 3);

					for (var i = 0; i < c; i++) {
						rooms.Add(RoomRegistry.Generate(RoomType.SubShop, biome));
					}
				}

				if (NpcSaveRoom.ShouldBeAdded()) {
					rooms.Add(new NpcSaveRoom());
					rooms.Add(new NpcKeyRoom());
				}

				TombRoom.Insert(rooms);
				biome.ModifyRooms(rooms);
			}

			return rooms;
		}

		protected virtual Painter GetPainter() {
			return new Painter();
		}

		protected virtual Builder GetBuilder() {
			Builder builder;

			if (IsFinal() || Run.Type == RunType.BossRush) {
				builder = new LineBuilder();
			} else {
				builder = LevelSave.BiomeGenerated.GetBuilder();

				if (builder is RegularBuilder b) {
					if (LevelSave.BiomeGenerated.Id == Biome.Ice) {
						b.SetTunnelLength(new float[] {4, 6, 4}, new float[] {1, 3, 1});
					} else if (GetFilling() == Tile.Chasm) {
						b.SetTunnelLength(new float[] {4, 3, 4}, new float[] {1, 3, 1});
					}
				}
			}

			return builder;
		}

		protected int GetNumConnectionRooms() {
			return 0;
		}
	}
}
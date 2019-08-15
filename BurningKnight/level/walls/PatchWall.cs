using System;
using BurningKnight.level.rooms;
using BurningKnight.level.tile;
using BurningKnight.util;
using BurningKnight.util.geometry;
using Lens.util;

namespace BurningKnight.level.walls {
	public class PatchWall : WallPainter {
		protected bool[] Patch;

		protected int ToIndex(RoomDef room, int x, int y) {
			return (x - room.Left - 1) + (y - room.Top - 1) * (room.GetWidth() - 2);
		}

		protected void Setup(Level level, RoomDef room, float fill, int clustering, bool ensurePath) {
			var w = room.GetWidth() - 2;
			var h = room.GetHeight() - 2;
			
			if (ensurePath) {
				PathFinder.SetMapSize(w, h);
				
				var valid = false;
				var attempt = 0;

				do {
					Patch = BurningKnight.level.Patch.Generate(w, h, fill, clustering);
					var start = 0;

					foreach (var d in room.Connected.Values) {
						if (d.X == room.Left) {
							start = ToIndex(room, d.X + 1, d.Y);
							
							Patch[ToIndex(room, d.X + 1, d.Y)] = false;
							Patch[ToIndex(room, d.X + 2, d.Y)] = false;
						} else if (d.X == room.Right) {
							start = ToIndex(room, d.X - 1, d.Y);
							
							Patch[ToIndex(room, d.X - 1, d.Y)] = false;
							Patch[ToIndex(room, d.X - 2, d.Y)] = false;
						} else if (d.Y == room.Top) {
							start = ToIndex(room, d.X, d.Y + 1);
							
							Patch[ToIndex(room, d.X, d.Y + 1)] = false;
							Patch[ToIndex(room, d.X, d.Y + 2)] = false;
						} else if (d.Y == room.Bottom) {
							start = ToIndex(room, d.X, d.Y - 1);
							
							Patch[ToIndex(room, d.X, d.Y - 1)] = false;
							Patch[ToIndex(room, d.X, d.Y - 2)] = false;
						}
					}
					
					PathFinder.BuildDistanceMap(start, BArray.Not(Patch, null));
					valid = true;

					for (var i = 0; i < Patch.Length; i++) {
						if (!Patch[i] && PathFinder.Distance[i] == Int32.MaxValue) {
							valid = false;
							break;
						}
					}
				} while (attempt++ < 100 && !valid);

				if (!valid) {
					Log.Error("Failed to generate patch");
				}
				
				PathFinder.SetMapSize(level.Width, level.Height);
			} else {
				Patch = BurningKnight.level.Patch.Generate(w, h, fill, clustering);
			}
		}

		protected void PaintPatch(Level level, RoomDef room, Tile tile) {
			var w = room.GetWidth() - 2;
			
			for (var y = 0; y < room.GetHeight() - 2; y++) {
				for (var x = 0; x < w; x++) {
					if (Patch[x + y * w]) {
						level.Set(room.Left + x + 1, room.Top + y + 1, tile);						
					}
				}
			}
		}

		protected void CleanDiagonalEdges(RoomDef room) {
			if (Patch == null) {
				return;
			}

			var w = room.GetWidth() - 2;

			for (var i = 0; i < Patch.Length - w; i++) {
				if (!Patch[i]) {
					continue;
				}

				if (i % w != 0) {
					if (Patch[i - 1 + w] && !(Patch[i - 1] || Patch[i + w])) {
						Patch[i - 1 + w] = false;
					}
				}

				if ((i + 1) % w != 0) {
					if (Patch[i + 1 + w] && !(Patch[i + 1] || Patch[i + w])) {
						Patch[i + 1 + w] = false;
					}
				}
			}
		}
		
		public override void Paint(Level level, RoomDef room, Rect inside) {
			var fill = 0.25f + (room.GetWidth() * room.GetHeight()) / 1024f;
			
			Setup(level, room, fill, 4, true);
			CleanDiagonalEdges(room);
			PaintPatch(level, room, Tile.WallA);
		}
	}
}
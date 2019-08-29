using BurningKnight.assets;
using BurningKnight.entity.component;
using Lens.graphics;
using Lens.graphics.animation;
using Microsoft.Xna.Framework;
using VelcroPhysics.Utilities;
using MathUtils = Lens.util.MathUtils;

namespace BurningKnight.entity.projectile {
	public class ProjectileGraphicsComponent : SliceComponent {
		public static TextureRegion Flash;
		
		public ProjectileGraphicsComponent(string image, string slice) : base(image, slice) {
			if (Flash == null) {
				Flash = CommonAse.Particles.GetSlice("flash");
			}
		}

		public override void Render(bool shadow) {
			var p = (Projectile) Entity;
			var scale = new Vector2(p.Scale);
			var a = p.GetAnyComponent<BodyComponent>().Body.Rotation;

			var spr = p.FlashTimer > 0 ? Flash : Sprite;
			
			if (shadow) {
				Graphics.Render(spr, Entity.Position + new Vector2(spr.Center.X, spr.Height + spr.Center.Y + 4), 
					a, spr.Center, scale);
				return;
			}

			var d = p.Dying || (p.IndicateDeath && p.T >= p.Range - 1.8f && p.T % 0.6f >= 0.3f);

			if (d) {
				var shader = Shaders.Entity;
				
				Shaders.Begin(shader);
				
				shader.Parameters["flash"].SetValue(1f);
				shader.Parameters["flashReplace"].SetValue(1f);
				shader.Parameters["flashColor"].SetValue(ColorUtils.White);
			}
			
			Graphics.Render(spr, Entity.Position + spr.Center, a, spr.Center, scale);

			if (d) {
				Shaders.End();
			}
		}
	}
}
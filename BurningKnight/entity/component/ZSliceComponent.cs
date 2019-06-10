using Lens.assets;
using Lens.entity.component.graphics;
using Lens.graphics;
using Lens.graphics.animation;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.component {
	public class ZSliceComponent : GraphicsComponent {
		public TextureRegion Sprite;
		
		public ZSliceComponent(string image, string slice) {
			Sprite = Animations.Get(image).GetSlice(slice);
		}

		public ZSliceComponent(AnimationData image, string slice) {
			Sprite = image.GetSlice(slice);
		}

		public override void Render(bool shadow) {
			var z = Entity.GetComponent<ZComponent>().Z;
			
			if (shadow) {
				Graphics.Render(Sprite, Entity.Position + new Vector2(0, Sprite.Height + z), 0, Vector2.Zero, Vector2.One, Graphics.ParseEffect(Flipped, !FlippedVerticaly));
				return;
			}
			
			Graphics.Render(Sprite, Entity.Position - new Vector2(0, z));
		}
	}
}
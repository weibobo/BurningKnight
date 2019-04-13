﻿using System;
using Lens.entity.component.graphics;

namespace Lens.entity.component.logic {
	public class StateComponent : Component {
		private EntityState state;
		private Type newState;
		
		public Type State {
			get => state.GetType();
			
			set {
				if (state == null || state.GetType() != value) {
					newState = value;
				}				
			}
		}

		public Type ForceState {
			set => newState = value;
		}

		public EntityState StateInstance => state;
		
		public void Become<T>(bool force = false) {
			if (force) {
				ForceState = typeof(T);
			} else {
				State = typeof(T);
			}
		}

		public override void Update(float dt) {
			base.Update(dt);

			if (newState != null) {
				state?.Destroy();
				
				state = (EntityState) Activator.CreateInstance(newState);
				state.Assign(Entity);
				state.Init();

				Send(new StateChangedEvent {
					NewState = newState,
					State = state
				});
				
				newState = null;
			}
			
			state?.Update(dt);
		}
	}
}
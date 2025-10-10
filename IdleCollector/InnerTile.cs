using IdleEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    internal class InnerTile: IUpdatable, IRenderable
    {
        private List<Interactable> interactables;
        private bool alreadyInteractedWith;
        private ResourceInfo grassResource;

        public EmptyCollider Collider { get; set; }
        public float Cooldown { get; set; }
        public int InteractableCount { get => interactables.Count; }
        public float LayerDepth { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Color Color { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public InnerTile(EmptyCollider collider)
        {
            Collider = collider;
            interactables = new();
            grassResource = new ResourceInfo("Grass");
            grassResource.Count = 1;
        }

        public void Add(Interactable interactable)
        {
            Cooldown = 1;
            interactables.Add(interactable);
        }

        public void ControlledUpdate(GameTime gameTime)
        {
            Cooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            //foreach (Interactable interactable in interactables)
            //    interactable.ControlledUpdate(gameTime);
        }

        public void SlowUpdate(GameTime gameTime)
        {
            //foreach (Interactable interactable in interactables)
            //    interactable.SlowUpdate(gameTime);
        }

        public void StandardUpdate(GameTime gameTime)
        {
            foreach (Interactable interactable in interactables)
                interactable.StandardUpdate(gameTime);
        }

        public void Draw(SpriteBatch sb)
        {
            foreach (Interactable interactable in interactables)
                interactable.Draw(sb);
        }
        public void ApplyWind(Vector2 windScroll, FastNoiseLite noise)
        {
            for (int j = 0; j < interactables.Count; j++)
            {
                interactables[j].ApplyWind(windScroll, noise);
            }
        }

        public void InteractWith(Entity entity)
        {
            foreach (Interactable interactable in interactables)
                interactable.InteractWith(entity);

            SecondaryInteractWith(entity);
        }
        public void SecondaryInteractWith(Entity entity)
        {
            if (interactables.Count == 0) return;
            if (!(Cooldown <= 0 && !alreadyInteractedWith)) return;

            alreadyInteractedWith = true;
            ResourceManager.Instance.SpawnResourceUIObj(Collider.Position, grassResource);

            foreach (Interactable interactable in interactables)
                interactable.SecondaryInteractWith(entity);
        }
    }
}

using Microsoft.Xna.Framework;

namespace TwinStickShooter.Entities
{
    /// <summary>
    /// Sistema de partículas puramente struct-based: un array de tamaño fijo
    /// creado una sola vez. No usa ObjectPool porque las partículas son datos
    /// puros (no tienen identidad ni comportamiento propio) — un struct[]
    /// con flag Active alcanza y evita el overhead de instancias por clase.
    /// Emit()/Update() nunca allocan.
    /// </summary>
    public class ParticleSystem
    {
        public struct Particle
        {
            public bool Active;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
            public float MaxLife;
            public float Size;
            public Color Color;
        }

        private readonly Particle[] _particles;

        public ParticleSystem(int capacity)
        {
            _particles = new Particle[capacity];
        }

        /// <summary>Array fijo de partículas (activas e inactivas); usado por el renderer.</summary>
        public Particle[] Particles => _particles;

        public void Emit(Vector2 position, Vector2 velocity, float life, float size, Color color)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i].Active)
                {
                    continue;
                }

                _particles[i].Active = true;
                _particles[i].Position = position;
                _particles[i].Velocity = velocity;
                _particles[i].Life = life;
                _particles[i].MaxLife = life;
                _particles[i].Size = size;
                _particles[i].Color = color;
                return;
            }

            // Sistema lleno: se descarta la emisión en vez de crecer el array.
        }

        public void Update(float deltaTime)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                ref Particle p = ref _particles[i];
                if (!p.Active)
                {
                    continue;
                }

                p.Life -= deltaTime;
                if (p.Life <= 0f)
                {
                    p.Active = false;
                    continue;
                }

                p.Position += p.Velocity * deltaTime;
                p.Velocity *= 0.94f; // fricción simple: la partícula frena con el tiempo
            }
        }
    }
}

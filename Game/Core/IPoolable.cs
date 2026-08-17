using Microsoft.Xna.Framework;

namespace TwinStickShooter.Core
{
    /// <summary>
    /// Contrato para objetos administrados por ObjectPool&lt;T&gt;.
    /// Reset() reinicializa el estado al reutilizar la instancia, evitando
    /// el costo (y la presión sobre el GC) de crear un objeto nuevo.
    /// </summary>
    public interface IPoolable : IEntity
    {
        void Reset();
    }

    /// <summary>
    /// Contrato para entidades físicas en el mundo.
    /// Define propiedades básicas para movimiento, colisión y renderizado.
    /// </summary>
    public interface IEntity
    {
        Vector2 Position { get; set; }
        float Radius { get; }
        bool Active { get; set; }
        Color Color { get; set; }
    }
}

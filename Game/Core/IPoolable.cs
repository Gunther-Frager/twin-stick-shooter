namespace TwinStickShooter.Core
{
    /// <summary>
    /// Contrato para objetos administrados por ObjectPool&lt;T&gt;.
    /// Reset() reinicializa el estado al reutilizar la instancia, evitando
    /// el costo (y la presión sobre el GC) de crear un objeto nuevo.
    /// </summary>
    public interface IPoolable
    {
        void Reset();
    }
}

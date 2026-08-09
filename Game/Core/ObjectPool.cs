namespace TwinStickShooter.Core
{
    /// <summary>
    /// Pool genérico de tamaño fijo. Todas las instancias se crean UNA vez
    /// en el constructor; Acquire/Release solo mueven índices en una pila
    /// de libres (O(1), cero allocations en runtime).
    ///
    /// Uso típico: balas, partículas complejas, o cualquier entidad con
    /// identidad/comportamiento propio que se recicla en vez de destruirse.
    /// </summary>
    public class ObjectPool<T> where T : class, IPoolable, new()
    {
        private readonly T[] _items;
        private readonly int[] _freeStack;
        private int _freeCount;

        public int Capacity { get; }

        /// <summary>Array fijo con TODAS las instancias (activas e inactivas).
        /// Los consumidores iteran esto y filtran por su propio flag Active.</summary>
        public T[] Items => _items;

        public ObjectPool(int capacity)
        {
            Capacity = capacity;
            _items = new T[capacity];
            _freeStack = new int[capacity];

            for (int i = 0; i < capacity; i++)
            {
                _items[i] = new T();
                _freeStack[i] = capacity - 1 - i;
            }

            _freeCount = capacity;
        }

        /// <summary>
        /// Intenta obtener una instancia libre (ya reseteada). Devuelve false
        /// si el pool está lleno; el llamador decide qué hacer (ej: ignorar
        /// el spawn) en vez de que el pool crezca dinámicamente.
        /// </summary>
        public bool TryAcquire(out int index, out T item)
        {
            if (_freeCount == 0)
            {
                index = -1;
                item = null;
                return false;
            }

            index = _freeStack[--_freeCount];
            item = _items[index];
            item.Reset();
            return true;
        }

        /// <summary>Devuelve el índice al pool para que pueda reutilizarse.</summary>
        public void Release(int index)
        {
            _freeStack[_freeCount++] = index;
        }
    }
}

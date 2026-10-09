using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TwinStickShooter.Core;
using Xunit;

namespace TwinStickShooter.Tests;

public class ObjectPoolTests
{
    private sealed class TestPoolable : IPoolable
    {
        public Vector2 Position { get; set; }
        public float Radius => 5f;
        public bool Active { get; set; }
        public Color Color { get; set; }
        public int ResetCount { get; private set; }

        public void Reset()
        {
            Active = true;
            ResetCount++;
        }
    }

    [Fact]
    public void Constructor_ValidCapacity_CreatesDistinctInstancesAndMatchesLength()
    {
        const int capacity = 10;
        var pool = new ObjectPool<TestPoolable>(capacity);

        Assert.Equal(capacity, pool.Capacity);
        Assert.NotNull(pool.Items);
        Assert.Equal(capacity, pool.Items.Length);

        var distinctItems = new HashSet<TestPoolable>(pool.Items);
        Assert.Equal(capacity, distinctItems.Count);
        foreach (var item in pool.Items)
        {
            Assert.NotNull(item);
        }
    }

    [Fact]
    public void TryAcquire_InitialState_ReturnsIndicesInSequentialOrder()
    {
        const int capacity = 5;
        var pool = new ObjectPool<TestPoolable>(capacity);

        for (int i = 0; i < capacity; i++)
        {
            bool success = pool.TryAcquire(out int index, out TestPoolable item);
            Assert.True(success);
            Assert.Equal(i, index);
            Assert.Same(pool.Items[i], item);
        }
    }

    [Fact]
    public void TryAcquire_Success_CallsResetOnAcquiredItem()
    {
        var pool = new ObjectPool<TestPoolable>(3);

        bool success = pool.TryAcquire(out int index, out TestPoolable item);

        Assert.True(success);
        Assert.NotNull(item);
        Assert.True(item.Active);
        Assert.Equal(1, item.ResetCount);
    }

    [Fact]
    public void TryAcquire_PoolExhausted_ReturnsFalseWithMinusOneIndexAndNullItem()
    {
        const int capacity = 2;
        var pool = new ObjectPool<TestPoolable>(capacity);

        for (int i = 0; i < capacity; i++)
        {
            Assert.True(pool.TryAcquire(out _, out _));
        }

        bool result = pool.TryAcquire(out int index, out TestPoolable item);

        Assert.False(result);
        Assert.Equal(-1, index);
        Assert.Null(item);
    }

    [Fact]
    public void Release_ReleasedIndex_IsReusedInLifoOrder()
    {
        var pool = new ObjectPool<TestPoolable>(5);

        pool.TryAcquire(out int idx0, out _); // 0
        pool.TryAcquire(out int idx1, out _); // 1
        pool.TryAcquire(out int idx2, out _); // 2

        pool.Release(idx0);
        pool.Release(idx2);

        // LIFO: idx2 was released last, so idx2 is acquired next
        bool acq1 = pool.TryAcquire(out int next1, out _);
        Assert.True(acq1);
        Assert.Equal(idx2, next1);

        // Then idx0
        bool acq2 = pool.TryAcquire(out int next2, out _);
        Assert.True(acq2);
        Assert.Equal(idx0, next2);
    }

    [Fact]
    public void FullCycle_AcquireAllReleaseAllAcquireAll_SucceedsWithoutErrors()
    {
        const int capacity = 8;
        var pool = new ObjectPool<TestPoolable>(capacity);
        var acquiredIndices = new List<int>();

        // 1. Acquire all
        for (int i = 0; i < capacity; i++)
        {
            bool success = pool.TryAcquire(out int index, out TestPoolable item);
            Assert.True(success);
            Assert.NotNull(item);
            acquiredIndices.Add(index);
        }

        // Exhausted
        Assert.False(pool.TryAcquire(out _, out _));

        // 2. Release all
        foreach (int idx in acquiredIndices)
        {
            pool.Release(idx);
        }

        // 3. Acquire all again
        for (int i = 0; i < capacity; i++)
        {
            bool success = pool.TryAcquire(out int index, out TestPoolable item);
            Assert.True(success);
            Assert.NotNull(item);
            Assert.InRange(index, 0, capacity - 1);
        }

        // Exhausted again
        Assert.False(pool.TryAcquire(out _, out _));
    }
}
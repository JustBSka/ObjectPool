Object Pool
====

Simple and easy to understand object pool implementation.

Overview
----

You might need and object pool when you want to reduce garbage collections by reusing objects rather than creating and destroying them.
The goal of this project is to make an easy to use and understand object pool.

This implementation is thread safe, supports async and partially lockless.

Usage
----

1. Create an empty pool with a factory, or a pool with a fixed collection of objects, or a warmed up pool.

2. Take the object and use it.

3. Dispose the pool item to release the object.

```csharp

var pool = new ObjectPool<MyObject>(() => new MyObject());
using (pool.Take(out var item))
{
	// Use the item
}
```

Clearing Items
----

If your pooling type derives from IClearable, it will be cleared before returning to the pool.

```csharp
public class MyObject : IClearable
{
    public void Clear()
    {
        // Do your clearing here
    }
}
```
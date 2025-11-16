using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace ET
{
    [EntitySystemOf(typeof(DBComponent))]
    [FriendOf(typeof(DBComponent))]
    public static partial class DBComponentSystem
    {
        #region 性能优化：静态缓存

        /// <summary>
        /// 类型名称缓存
        /// </summary>
        private static class TypeNameCache<T>
        {
            public static readonly string FullName = typeof(T).FullName;
        }

        private const string MongoIdFieldName = "_id";
        private static readonly ReplaceOptions UpsertOptions = new() { IsUpsert = true };

        #endregion

        [EntitySystem]
        private static void Awake(this DBComponent self, string dbConnection, string dbName)
        {
            self.mongoClient = new MongoClient(dbConnection);
            self.database = self.mongoClient.GetDatabase(dbName);
        }

    /// <summary>
    /// 获取 MongoDB 集合（高性能：使用类型名称缓存）
    /// 
    /// 使用场景：需要使用 MongoDB 原生 API（排序、投影、聚合等）
    /// 
    /// 示例：
    /// // 排序查询
    /// var collection = db.GetCollection&lt;PlayerData&gt;();
    /// var cursor = await collection.Find(p => p.Level >= 10)
    ///     .SortByDescending(p => p.Level)
    ///     .Limit(100)
    ///     .ToCursorAsync();
    /// 
    /// // 投影查询（只返回部分字段）
    /// var projection = Builders&lt;PlayerData&gt;.Projection
    ///     .Include(p => p.Name)
    ///     .Include(p => p.Level);
    /// var cursor = await collection.Find(p => p.PlayerId == id)
    ///     .Project&lt;PlayerData&gt;(projection)
    ///     .ToCursorAsync();
    /// 
    /// 注意：MongoDB.Driver 内部已有集合缓存，性能已足够好
    /// </summary>
    public static IMongoCollection<T> GetCollection<T>(this DBComponent self, string collection = null) where T : class
    {
        string collectionName = collection ?? TypeNameCache<T>.FullName;
        return self.database.GetCollection<T>(collectionName);
    }
        
    #region Query

    /// <summary>
    /// 查询单个数据对象（根据业务条件）
    /// 示例：var player = await db.QuerySingle&lt;PlayerData&gt;(p => p.PlayerId == playerId);
    /// </summary>
    public static async ETTask<T> QuerySingle<T>(this DBComponent self, Expression<Func<T, bool>> filter, string collection = null)
            where T : class
    {
        using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.DB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
        {
            IAsyncCursor<T> cursor = await self.GetCollection<T>(collection).FindAsync(filter);
            return await cursor.FirstOrDefaultAsync();
        }
    }

    /// <summary>
    /// 查询单个数据对象，指定taskId用于协程锁（根据业务条件）
    /// </summary>
    public static async ETTask<T> QuerySingle<T>(this DBComponent self, long taskId, Expression<Func<T, bool>> filter, string collection = null)
            where T : class
    {
        using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.DB, taskId % DBComponent.TaskCount))
        {
            IAsyncCursor<T> cursor = await self.GetCollection<T>(collection).FindAsync(filter);
            return await cursor.FirstOrDefaultAsync();
        }
    }
        
    /// <summary>
    /// 查询多个数据对象（根据业务条件）
    /// 示例：var players = await db.Query&lt;PlayerData&gt;(p => p.Level >= 10);
    /// </summary>
    public static async ETTask<List<T>> Query<T>(this DBComponent self, Expression<Func<T, bool>> filter, string collection = null)
            where T : class
    {
        using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.DB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
        {
            IAsyncCursor<T> cursor = await self.GetCollection<T>(collection).FindAsync(filter);

            return await cursor.ToListAsync();
        }
    }

    /// <summary>
    /// 查询多个数据对象，指定taskId用于协程锁（根据业务条件）
    /// </summary>
    public static async ETTask<List<T>> Query<T>(this DBComponent self, long taskId, Expression<Func<T, bool>> filter, string collection = null)
            where T : class
    {
        using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.DB, taskId % DBComponent.TaskCount))
        {
            IAsyncCursor<T> cursor = await self.GetCollection<T>(collection).FindAsync(filter);

            return await cursor.ToListAsync();
        }
    }

    /// <summary>
    /// 使用JSON格式的查询条件查询数据（支持任何class类型）
    /// </summary>
    public static async ETTask<List<T>> QueryJson<T>(this DBComponent self, string json, string collection = null) where T : class
    {
        using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.DB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
        {
            FilterDefinition<T> filterDefinition = new JsonFilterDefinition<T>(json);
            IAsyncCursor<T> cursor = await self.GetCollection<T>(collection).FindAsync(filterDefinition);
            return await cursor.ToListAsync();
        }
    }

    /// <summary>
    /// 使用JSON格式的查询条件查询数据，指定taskId用于协程锁（支持任何class类型）
    /// </summary>
    public static async ETTask<List<T>> QueryJson<T>(this DBComponent self, long taskId, string json, string collection = null) where T : class
    {
        using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.DB, taskId % DBComponent.TaskCount))
        {
            FilterDefinition<T> filterDefinition = new JsonFilterDefinition<T>(json);
            IAsyncCursor<T> cursor = await self.GetCollection<T>(collection).FindAsync(filterDefinition);
            return await cursor.ToListAsync();
        }
    }

        #endregion

    #region Insert

    /// <summary>
    /// 批量插入数据（高性能：使用类型名称缓存）
    /// </summary>
    public static async ETTask InsertBatch<T>(this DBComponent self, IEnumerable<T> list, string collection = null) where T : class
    {
        string collectionName = collection ?? TypeNameCache<T>.FullName;
        
        using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.DB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
        {
            await self.database.GetCollection<T>(collectionName).InsertManyAsync(list);
        }
    }

    #endregion

    #region Save

    /// <summary>
    /// 保存数据对象（MongoDB 自动生成主键）。
    /// 适用于日志、临时数据等无需手动指定主键的场景。
    /// </summary>
    public static async ETTask Save<T>(this DBComponent self, T entity, string collection = null) where T : class
    {
        if (entity == null)
        {
            Log.Error($"save entity is null: {TypeNameCache<T>.FullName}");
            return;
        }

        string collectionName = collection ?? TypeNameCache<T>.FullName;
        long lockKey = RandomGenerator.RandInt64();

        using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.DB, lockKey % DBComponent.TaskCount))
        {
            await self.database.GetCollection<T>(collectionName).InsertOneAsync(entity);
        }
    }

    /// <summary>
    /// 保存数据对象（显式主键，零装箱）。推荐业务调用此重载。
    /// </summary>
    public static async ETTask Save<T, TKey>(this DBComponent self, T entity, TKey primaryKey, string collection = null) where T : class
    {
        if (primaryKey == null)
        {
            Log.Error($"save primaryKey is null: {TypeNameCache<T>.FullName}");
            return;
        }

        await SaveWithPrimaryKey(self, entity, collection, primaryKey, primaryKey.GetHashCode());
    }

    /// <summary>
    /// 保存数据对象（显式主键 + taskId，便于批量操作自定义协程锁分片）。
    /// </summary>
    public static async ETTask Save<T, TKey>(this DBComponent self, long taskId, T entity, TKey primaryKey, string collection = null) where T : class
    {
        if (primaryKey == null)
        {
            Log.Error($"save primaryKey is null: {TypeNameCache<T>.FullName}");
            return;
        }

        await SaveWithPrimaryKey(self, entity, collection, primaryKey, null, taskId);
    }

    /// <summary>
    /// 保存数据对象（Fire-and-Forget，显式主键）。
    /// </summary>
    public static ETTask SaveNotWait<T, TKey>(this DBComponent self, T entity, TKey primaryKey, long taskId = 0, string collection = null) where T : class
    {
        return taskId == 0
            ? self.Save(entity, primaryKey, collection)
            : self.Save(taskId, entity, primaryKey, collection);
    }

    /// <summary>
    /// 保存数据对象（Fire-and-Forget，MongoDB 自动生成主键）。
    /// </summary>
    public static ETTask SaveNotWait<T>(this DBComponent self, T entity, string collection = null) where T : class
    {
        return self.Save(entity, collection);
    }

    /// <summary>
    /// 保存数据对象（兼容入口）。primaryKey 不为空时转调泛型零装箱版本。
    /// </summary>
    public static async ETTask Save<T>(this DBComponent self, T entity, object primaryKey, string collection = null) where T : class
    {
        if (primaryKey == null)
        {
            await self.Save(entity, collection);
            return;
        }

        await self.Save<T, object>(entity, primaryKey, collection);
    }

    /// <summary>
    /// 保存数据对象（兼容入口 + taskId）。
    /// </summary>
    public static async ETTask Save<T>(this DBComponent self, long taskId, T entity, object primaryKey, string collection = null) where T : class
    {
        if (primaryKey == null)
        {
            await self.Save(entity, collection);
            return;
        }

        await self.Save<T, object>(taskId, entity, primaryKey, collection);
    }

    /// <summary>
    /// 保存数据对象（Fire-and-Forget 兼容入口）。
    /// </summary>
    public static ETTask SaveNotWait<T>(this DBComponent self, T entity, object primaryKey, long taskId = 0, string collection = null) where T : class
    {
        return taskId == 0
            ? self.Save(entity, primaryKey, collection)
            : self.Save(taskId, entity, primaryKey, collection);
    }

    private static async ETTask SaveWithPrimaryKey<T, TKey>(DBComponent self, T entity, string collection, TKey primaryKey, int? lockKeyHash = null, long? taskId = null) where T : class
    {
        if (entity == null)
        {
            Log.Error($"save entity is null: {TypeNameCache<T>.FullName}");
            return;
        }

        string collectionName = collection ?? TypeNameCache<T>.FullName;
        CoroutineLockComponent coroutineLockComponent = self.Root().GetComponent<CoroutineLockComponent>();

        long lockShard = taskId.HasValue
            ? taskId.Value % DBComponent.TaskCount
            : (lockKeyHash ?? primaryKey!.GetHashCode()) % DBComponent.TaskCount;

        using (await coroutineLockComponent.Wait(CoroutineLockType.DB, lockShard))
        {
            await self.database.GetCollection<T>(collectionName).ReplaceOneAsync(
                Builders<T>.Filter.Eq(MongoIdFieldName, primaryKey),
                entity,
                UpsertOptions
            );
        }
    }

        #endregion

    #region Remove
    
    /// <summary>
    /// 删除单个数据对象（根据业务条件）
    /// 示例：await db.RemoveSingle&lt;PlayerData&gt;(p => p.PlayerId == playerId);
    /// </summary>
    public static async ETTask<bool> RemoveSingle<T>(this DBComponent self, Expression<Func<T, bool>> filter, string collection = null) where T : class
    {
        using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.DB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
        {
            DeleteResult result = await self.GetCollection<T>(collection).DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }
    }

    /// <summary>
    /// 删除单个数据对象，指定taskId用于协程锁（根据业务条件）
    /// </summary>
    public static async ETTask<bool> RemoveSingle<T>(this DBComponent self, long taskId, Expression<Func<T, bool>> filter, string collection = null) where T : class
    {
        using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.DB, taskId % DBComponent.TaskCount))
        {
            DeleteResult result = await self.GetCollection<T>(collection).DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }
    }

    /// <summary>
    /// 删除多个数据对象（根据业务条件）
    /// 示例：await db.Remove&lt;PlayerData&gt;(p => p.Level < 10);
    /// </summary>
    public static async ETTask<long> Remove<T>(this DBComponent self, Expression<Func<T, bool>> filter, string collection = null) where T : class
    {
        using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.DB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
        {
            DeleteResult result = await self.GetCollection<T>(collection).DeleteManyAsync(filter);
            return result.DeletedCount;
        }
    }

    /// <summary>
    /// 删除多个数据对象，指定taskId用于协程锁（根据业务条件）
    /// </summary>
    public static async ETTask<long> Remove<T>(this DBComponent self, long taskId, Expression<Func<T, bool>> filter, string collection = null) where T : class
    {
        using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.DB, taskId % DBComponent.TaskCount))
        {
            DeleteResult result = await self.GetCollection<T>(collection).DeleteManyAsync(filter);
            return result.DeletedCount;
        }
    }

    #endregion
    }
}
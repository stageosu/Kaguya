﻿using System.Threading.Tasks;

namespace Kaguya.Database.Interfaces
{
    public interface IRepository<in TKey, TValue>
    {
        public Task<TValue> GetAsync(TKey key);
        public Task DeleteAsync(TKey key);
        public Task<TValue> UpdateAsync(TKey key, TValue value);
        public Task InsertAsync(TValue value);
    }
}
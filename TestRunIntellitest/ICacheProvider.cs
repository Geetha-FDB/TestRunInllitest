using System;

namespace FDB.CC.Screening.Models.V1_4.Common
{
    public interface ICacheProvider
    {
        bool Set(string key, object value, TimeSpan slidingExpiration);
        bool Set(string key, object value, DateTime expiration);

        bool Set(string key, object value, DateTimeOffset expiration);
        object Get(string key);
        bool TryGetValue(string key, out object result);

    }
}

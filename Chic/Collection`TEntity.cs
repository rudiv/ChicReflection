using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Chic
{
    public class Collection<TEntity> : IQueryable<TEntity>
        where TEntity : class
    {
        public Type ElementType => typeof(TEntity);

        public Expression Expression => throw new NotImplementedException();

        public IQueryProvider Provider { get; }

        public Collection(IQueryProvider queryProvider)
        {
            Provider = queryProvider;
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}

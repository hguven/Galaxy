﻿using EventStore.ClientAPI;
using Galaxy.Domain;
using Galaxy.Infrastructure;
using Galaxy.Repositories;
using Galaxy.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Galaxy.EventStore
{
    public class AggregateRootRepository<TAggregateRoot, TKey> : IRepositoryAsync<TAggregateRoot, TKey>, IRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot, IObjectState
    {

        private readonly IUnitOfWorkAsync _unitOfworkAsync;
        public AggregateRootRepository(IUnitOfWorkAsync unitOfworkAsync)
        {
            _unitOfworkAsync = unitOfworkAsync ?? throw new ArgumentNullException(nameof(unitOfworkAsync));
        }

        public void Delete(object id)
        {
            throw new NotImplementedException();
        }

        public void Delete(TAggregateRoot entity)
        {
            _unitOfworkAsync.Attach(entity);
        }

        public Task<bool> DeleteAsync(params object[] keyValues)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(CancellationToken cancellationToken, params object[] keyValues)
        {
            throw new NotImplementedException();
        }

        public IQueryable<TAggregateRoot> ExecuteQuery(string sqlQuery)
        {
            throw new NotImplementedException();
        }

        public TAggregateRoot Find(params object[] keyValues)
        {
            throw new NotImplementedException();
        }

        public Task<TAggregateRoot> FindAsync(params object[] keyValues)
        {
            throw new NotImplementedException();
        }

        public Task<TAggregateRoot> FindAsync(CancellationToken cancellationToken, params object[] keyValues)
        {
            throw new NotImplementedException();
        }

        public void Insert(TAggregateRoot entity)
        {
            this._unitOfworkAsync.Attach(new Aggregate(Guid.NewGuid().ToString(), (int)ExpectedVersion.NoStream, entity));
        }

        public void InsertGraphRange(IEnumerable<TAggregateRoot> entities)
        {
            foreach (var entity in entities)
            {
                this._unitOfworkAsync
                    .Attach(new Aggregate(Guid.NewGuid().ToString(), (int)ExpectedVersion.NoStream, entity));
            }
        }

        public void InsertOrUpdateGraph(TAggregateRoot entity)
        {
            throw new NotImplementedException();
        }

        public void InsertRange(IEnumerable<TAggregateRoot> entities)
        {
            throw new NotImplementedException();
        }

        public IQueryFluent<TAggregateRoot> Query(IQueryObject<TAggregateRoot> queryObject)
        {
            throw new NotImplementedException();
        }

        public IQueryFluent<TAggregateRoot> Query(Expression<Func<TAggregateRoot, bool>> query)
        {
            throw new NotImplementedException();
        }

        public IQueryFluent<TAggregateRoot> Query()
        {
            throw new NotImplementedException();
        }

        public IQueryable<TAggregateRoot> Queryable()
        {
            throw new NotImplementedException();
        }

        public IQueryable<TAggregateRoot> QueryableNoTrack()
        {
            throw new NotImplementedException();
        }

        public IQueryable<TAggregateRoot> QueryableWithNoFilter()
        {
            throw new NotImplementedException();
        }

        public IQueryable<TAggregateRoot> SelectQuery(string query, params object[] parameters)
        {
            throw new NotImplementedException();
        }

        public void Update(TAggregateRoot entity)
        {
            throw new NotImplementedException();
        }

        IRepository<T> IRepository<TAggregateRoot, TKey>.GetRepository<T>()
        {
            throw new NotImplementedException();
        }
    }
}
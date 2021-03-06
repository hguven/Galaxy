﻿using Galaxy.Auditing;
using Galaxy.DataContext;
using Galaxy.Domain;
using Galaxy.Domain.Auditing;
using Galaxy.EFCore;
using Galaxy.EFCore.Extensions;
using Galaxy.EntityFrameworkCore;
using Galaxy.Infrastructure;
using Galaxy.Session;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Galaxy.Identity
{
    public abstract class GalaxyIdentityDbContext<TUser, TRole, TPrimaryKey> : IdentityDbContext<TUser, TRole, TPrimaryKey>, IGalaxyContextAsync
          where TUser : IdentityUser<TPrimaryKey>
          where TRole : IdentityRole<TPrimaryKey>
          where TPrimaryKey : IEquatable<TPrimaryKey>   
    {
        #region Private Fields 
        private readonly IAppSessionContext _appSession;
        bool _disposed;
        #endregion Private Fields

        protected static MethodInfo ConfigureGlobalFiltersMethodInfo = typeof(GalaxyIdentityDbContext<,,>).MakeGenericType(typeof(TUser), typeof(TRole), typeof(TPrimaryKey))
                                                                                                          .GetMethod(nameof(ConfigureGlobalFilters)
            , BindingFlags.Instance | BindingFlags.NonPublic) ;


        protected virtual string DEFAULT_SCHEMA { get; set; } = "identity";
        
        public GalaxyIdentityDbContext(DbContextOptions options) : base(options)
        { 
        }

        public GalaxyIdentityDbContext(DbContextOptions options, IAppSessionContext appSession) : base(options)
        {  
            this._appSession = appSession ?? throw new ArgumentNullException(nameof(appSession));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyAllConfigurationsFromCurrentAssembly();

            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims", DEFAULT_SCHEMA);
            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins", DEFAULT_SCHEMA);
            modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRoles", DEFAULT_SCHEMA);
            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims", DEFAULT_SCHEMA);
            modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens", DEFAULT_SCHEMA);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                ConfigureGlobalFiltersMethodInfo
                  .MakeGenericMethod(entityType.ClrType)
                   .Invoke(this, new object[] { entityType, modelBuilder });
            }
        }

        public new void Attach(object entity)
        {
            base.Attach(entity);
        }

        public override int SaveChanges()
        {
            SyncObjectsStatePreCommit();
            var changes = base.SaveChanges();
            SyncObjectsStatePostCommit();
            return changes;
        }

        public virtual bool CheckIfThereIsAvailableTransaction()
        {
            return !ChangeTracker.Entries().All(e => e.State == EntityState.Unchanged);
        }

        public virtual int SaveChangesByPassed()
        {
            var changes = base.SaveChanges();
            return changes;
        }

        public virtual async Task<int> SaveChangesByPassedAsync()
        {
            return await base.SaveChangesAsync();
        }

        public virtual async Task<int> SaveChangesByPassedAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task<int> SaveChangesAsync()
        {
            return await this.SaveChangesAsync(CancellationToken.None);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            this.SyncObjectsStatePreCommit();
            var changesAsync = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            this.SyncObjectsStatePostCommit();
            return changesAsync;
        }

        public virtual void SyncObjectState<TEntity>(TEntity entity) where TEntity : class, IObjectState
        {
            Entry(entity).State = StateHelper.ConvertState(entity.ObjectState);
        }

        public virtual void SyncEntityState<TEntity>(TEntity entity) where TEntity : class, IObjectState
        {
            entity.SyncObjectState(StateHelper.ConvertState(Entry(entity).State));
        }

        public virtual void SyncObjectsStatePreCommit()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        SetConcurrencyStampIfNull(entry);
                        break;
                    case EntityState.Modified:
                        UpdateConcurrencyStamp(entry);
                        break;
                    case EntityState.Deleted:
                        UpdateConcurrencyStamp(entry);
                        break;
                }
            }
        }

        public virtual void SyncObjectsAuditPreCommit(IAppSessionContext session)
        {
            if (!ChangeTracker.Entries().Any(e => (e.Entity is IAudit)))
                return;

            foreach (var dbEntityEntry in ChangeTracker.Entries<IAudit>())
            {
                var entity = (dbEntityEntry.Entity);

                if ((dbEntityEntry.State) == EntityState.Unchanged)
                    continue;

                if ((dbEntityEntry.State) == EntityState.Added)
                {
                    if (typeof(IMultiTenant).IsAssignableFrom(entity.GetType()))
                    {
                        ApplyTenantState(entity as IMultiTenant, session);
                        ApplyCreatedAuditState(entity, session);
                    }
                    else
                    {
                        ApplyCreatedAuditState(entity, session);
                    }
                }
                else
                {
                    if (typeof(IMultiTenant).IsAssignableFrom(entity.GetType()))
                    {
                        ApplyTenantState(entity as IMultiTenant, session);
                        ApplyUpdatedAuditState(entity, session);
                    }
                    else
                    {
                        ApplyUpdatedAuditState(entity, session);
                    }

                }
            }
        }

        public virtual void SyncObjectsStatePostCommit()
        {
            foreach (var dbEntityEntry in ChangeTracker.Entries())
            {
                ((IObjectState)dbEntityEntry.Entity).SyncObjectState(StateHelper.ConvertState(dbEntityEntry.State));
            }
        }

        private void ApplyCreatedAuditState(IAudit entity, IAppSessionContext session)
        {
            entity.SyncAuditState(creatorUserId: session.UserId, creationTime: DateTime.Now);
        }

        private void ApplyUpdatedAuditState(IAudit entity, IAppSessionContext session)
        {
            entity.SyncAuditState(lastmodifierUserId: session.UserId, lastModificationTime: DateTime.Now
                              , creatorUserId: entity.CreatorUserId, creationTime: entity.CreationTime);
        }

        private void ApplyTenantState(IMultiTenant entity, IAppSessionContext session)
        {
            entity.SyncTenantState(session.TenantId);
        }

        private void UpdateConcurrencyStamp(EntityEntry entry)
        {
            var entity = entry.Entity as IConcurrencyStamp;

            if (entity == null)
                return;

            Entry(entity).Property(x => x.ConcurrencyStamp).OriginalValue = entity.ConcurrencyStamp;
            entity.SyncConcurrencyStamp(Guid.NewGuid().ToString("N"));
        }

        private void SetConcurrencyStampIfNull(EntityEntry entry)
        {
            var entity = entry.Entity as IConcurrencyStamp;
            if (entity == null)
                return;

            if (entity.ConcurrencyStamp != null)
                return;

            entity.SyncConcurrencyStamp(Guid.NewGuid().ToString("N"));
        }


        public virtual async Task DispatchNotificationsAsync(IMediator mediator)
        {
            var notifications = ChangeTracker
                .Entries<IEntity>()
                .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any());

            var domainEvents = notifications
                .SelectMany(x => x.Entity.DomainEvents)
                .ToList();

            notifications.ToList()
                .ForEach(entity => entity.Entity.ClearEvents());

            var tasks = domainEvents
                .Select(async (domainEvent) =>
                {
                    await mediator.Publish(domainEvent);
                });

            await Task.WhenAll(tasks);

        }


        protected virtual void ConfigureGlobalFilters<TEntity>(IMutableEntityType entityType, ModelBuilder modelBuilder)
           where TEntity : class
        {
            if (entityType.BaseType == null && ShouldFilterEntity<TEntity>(entityType))
            {
                var filterExpression = CreateFilterExpression<TEntity>();
                if (filterExpression != null)
                {
                    modelBuilder.Entity<TEntity>().HasQueryFilter(filterExpression);
                }
            }
        }

        protected virtual Expression<Func<TEntity, bool>> CreateFilterExpression<TEntity>()
          where TEntity : class
        {
            Expression<Func<TEntity, bool>> expression = null;
            if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
            {
                Expression<Func<TEntity, bool>> softDeleteFilter = e => !((ISoftDelete)e).IsDeleted;
                expression = expression == null ? softDeleteFilter : CombineExpressions(expression, softDeleteFilter);
            }
            if (typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity)))
            {
                Expression<Func<TEntity, bool>> tenanFilter = e => ((IMultiTenant)e).TenantId == this._appSession.TenantId
                                                                || (((IMultiTenant)e).TenantId == this._appSession.TenantId) == this._appSession.TenantId.HasValue;
                expression = expression == null ? tenanFilter : CombineExpressions(expression, tenanFilter);
            }
            return expression;
        }

        protected virtual bool ShouldFilterEntity<TEntity>(object entityType) where TEntity : class
        {
            if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
            {
                return true;
            }
            if (typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity)))
            {
                return true;
            }
            return false;
        }

        protected virtual Expression<Func<T, bool>> CombineExpressions<T>(Expression<Func<T, bool>> expression1, Expression<Func<T, bool>> expression2)
        {
            var parameter = Expression.Parameter(typeof(T));

            var leftVisitor = new GalaxyExpressionVisitor(expression1.Parameters[0], parameter);
            var left = leftVisitor.Visit(expression1.Body);

            var rightVisitor = new GalaxyExpressionVisitor(expression2.Parameters[0], parameter);
            var right = rightVisitor.Visit(expression2.Body);

            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left, right), parameter);
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {

                    // free other managed objects that implement
                    // IDisposable only
                }

                // release any unmanaged objects
                // set object references to null

                _disposed = true;
            }

            Dispose(disposing);
        }


    }
}

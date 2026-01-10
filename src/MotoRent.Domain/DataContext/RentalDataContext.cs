using System.Linq.Expressions;
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.DataContext;

public partial class RentalDataContext
{
    public Query<Shop> Shops { get; }
    public Query<Renter> Renters { get; }
    public Query<Document> Documents { get; }
    public Query<Motorbike> Motorbikes { get; }
    public Query<Rental> Rentals { get; }
    public Query<Deposit> Deposits { get; }
    public Query<Insurance> Insurances { get; }
    public Query<Accessory> Accessories { get; }
    public Query<RentalAccessory> RentalAccessories { get; }
    public Query<Payment> Payments { get; }
    public Query<DamageReport> DamageReports { get; }
    public Query<DamagePhoto> DamagePhotos { get; }
    public Query<RentalAgreement> RentalAgreements { get; }
    public Query<ServiceType> ServiceTypes { get; }
    public Query<MaintenanceSchedule> MaintenanceSchedules { get; }

    private QueryProvider QueryProvider { get; }

    public RentalDataContext() : this(ObjectBuilder.GetObject<QueryProvider>()) { }

    public RentalDataContext(QueryProvider provider)
    {
        QueryProvider = provider;
        Shops = new Query<Shop>(provider);
        Renters = new Query<Renter>(provider);
        Documents = new Query<Document>(provider);
        Motorbikes = new Query<Motorbike>(provider);
        Rentals = new Query<Rental>(provider);
        Deposits = new Query<Deposit>(provider);
        Insurances = new Query<Insurance>(provider);
        Accessories = new Query<Accessory>(provider);
        RentalAccessories = new Query<RentalAccessory>(provider);
        Payments = new Query<Payment>(provider);
        DamageReports = new Query<DamageReport>(provider);
        DamagePhotos = new Query<DamagePhoto>(provider);
        RentalAgreements = new Query<RentalAgreement>(provider);
        ServiceTypes = new Query<ServiceType>(provider);
        MaintenanceSchedules = new Query<MaintenanceSchedule>(provider);
    }

    public async Task<T?> LoadOneAsync<T>(Expression<Func<T, bool>> predicate) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.LoadOneAsync(predicate);
    }

    public async Task<LoadOperation<T>> LoadAsync<T>(IQueryable<T> query,
        int page = 1, int size = 40, bool includeTotalRows = false) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.LoadAsync(query, page, size, includeTotalRows);
    }

    public PersistenceSession OpenSession(string username = "system")
    {
        return new PersistenceSession(this, username);
    }

    internal async Task<SubmitOperation> SubmitChangesAsync(PersistenceSession session, string operation, string username)
    {
        int inserted = 0, updated = 0, deleted = 0;

        try
        {
            // Process deletes first
            foreach (var entity in session.DeletedCollection)
            {
                await DeleteEntityAsync(entity);
                deleted++;
            }

            // Process attached entities (insert or update)
            foreach (var entity in session.AttachedCollection)
            {
                if (entity.GetId() == 0)
                {
                    await InsertEntityAsync(entity, username);
                    inserted++;
                }
                else
                {
                    await UpdateEntityAsync(entity, username);
                    updated++;
                }
            }

            // TODO: Publish message to RabbitMQ if messaging is configured
            // PublishMessage(session.AttachedCollection, operation);

            return SubmitOperation.CreateSuccess(inserted, updated, deleted);
        }
        catch (Exception ex)
        {
            return SubmitOperation.CreateFailure($"Submit failed: {ex.Message}", ex);
        }
    }

    private async Task InsertEntityAsync(Entity entity, string username)
    {
        var method = typeof(RentalDataContext).GetMethod(nameof(InsertTypedAsync),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var genericMethod = method!.MakeGenericMethod(entity.GetType());
        await (Task)genericMethod.Invoke(this, [entity, username])!;
    }

    private async Task InsertTypedAsync<T>(T entity, string username) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        await repos.InsertAsync(entity, username);
    }

    private async Task UpdateEntityAsync(Entity entity, string username)
    {
        var method = typeof(RentalDataContext).GetMethod(nameof(UpdateTypedAsync),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var genericMethod = method!.MakeGenericMethod(entity.GetType());
        await (Task)genericMethod.Invoke(this, [entity, username])!;
    }

    private async Task UpdateTypedAsync<T>(T entity, string username) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        await repos.UpdateAsync(entity, username);
    }

    private async Task DeleteEntityAsync(Entity entity)
    {
        var method = typeof(RentalDataContext).GetMethod(nameof(DeleteTypedAsync),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var genericMethod = method!.MakeGenericMethod(entity.GetType());
        await (Task)genericMethod.Invoke(this, [entity])!;
    }

    private async Task DeleteTypedAsync<T>(T entity) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        await repos.DeleteAsync(entity);
    }
}

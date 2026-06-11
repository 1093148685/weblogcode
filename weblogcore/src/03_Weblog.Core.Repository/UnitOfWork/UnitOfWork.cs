using SqlSugar;

namespace Weblog.Core.Repository.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ISqlSugarClient _db;

    public UnitOfWork(ISqlSugarClient db)
    {
        _db = db;
    }

    public ISqlSugarClient GetDbClient() => _db;

    public void BeginTran()
    {
        _db.Ado.BeginTran();
    }

    public void CommitTran()
    {
        _db.Ado.CommitTran();
    }

    public void RollbackTran()
    {
        _db.Ado.RollbackTran();
    }
}

public interface IUnitOfWork
{
    ISqlSugarClient GetDbClient();
    void BeginTran();
    void CommitTran();
    void RollbackTran();
}

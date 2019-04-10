using System;
using System.Data;
using Miningcore.Persistence.Model;
using Miningcore.Persistence.Model.Projections;
using System.Threading.Tasks;

namespace Miningcore.Persistence.Repositories
{
    public interface IInvalidShareRepository
    {
        Task InsertAsync(IDbConnection con, IDbTransaction tx, InvalidShare share);
        Task<long> CountInvalidSharesBetweenCreated(IDbConnection con, string poolId, string miner, DateTime? start, DateTime? end);
    }
}

using System.Data;
using System.Threading.Tasks;
using Miningcore.Configuration;
using Miningcore.Persistence.Model;

namespace Miningcore.Persistence.Repositories
{
    public interface IMinerInfoRepository
    {
        Task<int> AddMinerInfo(IDbConnection con, IDbTransaction tx, string poolId, string address, decimal minimumPayment);
        Task<MinerInfo> GetMinerInfo(IDbConnection con, IDbTransaction tx, string poolId, string address);
        Task<int> DeleteMinerInfo(IDbConnection con, IDbTransaction tx, string poolId, string address);
    }
}

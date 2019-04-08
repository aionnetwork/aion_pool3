using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Dapper;
using Miningcore.Extensions;
using Miningcore.Persistence.Model;
using Miningcore.Persistence.Model.Projections;
using Miningcore.Persistence.Repositories;
using Miningcore.Util;
using NLog;

namespace Miningcore.Persistence.Postgres.Repositories
{
    public class InvalidShareRepository : IInvalidShareRepository
    {
        public InvalidShareRepository(IMapper mapper)
        {
            this.mapper = mapper;
        }

        private readonly IMapper mapper;
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public void Insert(IDbConnection con, IDbTransaction tx, InvalidShare share)
        {
            logger.LogInvoke();

            if(String.IsNullOrEmpty(share.Miner) ||
                String.IsNullOrEmpty(share.PoolId) ||
                share.Created == null) {
                logger.Debug("Not storing invalid share.");
                return;
            }

            var mapped = mapper.Map<Entities.InvalidShare>(share);

            var query = "INSERT INTO invalid_shares(poolid, miner, worker, created) " +
                "VALUES(@poolid, @miner, @worker, @created)";

            con.Execute(query, mapped, tx);
        }

        public async Task<long> CountInvalidSharesBetweenCreated(IDbConnection con, string poolId, string miner, DateTime? start, DateTime? end)
        {
            logger.LogInvoke(new[] { poolId });

            var whereClause = "poolid = @poolId AND miner = @miner";

            if (start.HasValue)
                whereClause += " AND created >= @start ";
            if (end.HasValue)
                whereClause += " AND created <= @end";

            var query = $"SELECT count(*) FROM invalid_shares WHERE {whereClause}";

            return con.QuerySingle<long>(query, new { poolId, miner, start, end });
        }
    }
}

/*
Copyright 2017 Coin Foundry (coinfoundry.org)
Authors: Oliver Weichhold (oliver@weichhold.com)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial
portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Dapper;
using Miningcore.Extensions;
using Miningcore.Persistence.Model;
using Miningcore.Persistence.Repositories;
using NLog;

namespace Miningcore.Persistence.Postgres.Repositories
{
    public class MinerInfoRepository : IMinerInfoRepository
    {
        public MinerInfoRepository(IMapper mapper)
        {
            this.mapper = mapper;
        }

        private readonly IMapper mapper;
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public async Task<int> AddMinerInfo(IDbConnection con, IDbTransaction tx, string poolId, string address, decimal minimumPayment)
        {
            var query = "INSERT INTO miner_info(poolid, miner, minimumpayment) " +
                "VALUES(@poolid, @miner, @minimumpayment)";

            var minerInfoAdd = new Entities.MinerInfo
            {
                PoolId = poolId,
                Miner = address,
                MinimumPayment = minimumPayment
            };

            return await con.ExecuteAsync(query, minerInfoAdd, tx);
        }

        public async Task<int> DeleteMinerInfo(IDbConnection con, IDbTransaction tx, string poolId, string address)
        {
            var query = "DELETE FROM miner_info WHERE poolid = @poolId and miner = @address";

            return await con.ExecuteAsync(query, new { poolId, address}, tx);
        }

        public async Task<MinerInfo> GetMinerInfo(IDbConnection con, IDbTransaction tx, string poolId, string address)
         {
            logger.LogInvoke();

            const string query = "SELECT * FROM miner_info WHERE poolid = @poolId AND miner = @address";

            return (await con.QueryAsync<MinerInfo>(query, new { poolId, address }, tx)).FirstOrDefault();
        }
     }
}

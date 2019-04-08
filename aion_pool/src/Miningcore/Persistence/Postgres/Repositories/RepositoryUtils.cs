using System;
using System.Data;
using System.Linq;
using Dapper;
using AutoMapper;
using Miningcore.Persistence.Model;

namespace Miningcore.Persistence.Postgres.Repositories
{
    public class RepositoryUtils
    {
        public static StatsGranularity GetStatsGranularityFromQuery(IDbConnection con, string dateDiffQuery, Object parameters) 
        {
            var minMaxDates = con.Query<(DateTime, DateTime)>(dateDiffQuery, parameters).ToArray();
            var (startDate, endDate) = minMaxDates[0];
            var dayDif = endDate.Subtract(startDate).TotalDays;
            if (dayDif < 2)
                return StatsGranularity.Minutely;
            if (dayDif <= 7)
                return StatsGranularity.Hourly;
            if (dayDif <= 31)
                return StatsGranularity.Hourly;
            return StatsGranularity.Daily;
        }
        public static (string, string) GetSelectAndGroupStatements(StatsGranularity granularity)
        {
            switch (granularity)
            {
                case StatsGranularity.Minutely:
                    return ("SELECT date_trunc('minute', created) AS timestamp, ", "GROUP BY date_trunc('minute', created) ORDER BY timestamp;");
                case StatsGranularity.Hourly:
                    return ("SELECT date_trunc('hour', created) AS timestamp, ", "GROUP BY date_trunc('hour', created) ORDER BY timestamp;");
                default:
                    return ("SELECT date_trunc('day', created) AS timestamp, ", "GROUP BY date_trunc('day', created) ORDER BY timestamp;");
            }
        }
    }
}
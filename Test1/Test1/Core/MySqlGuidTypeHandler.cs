using System;
using System.Data;
using Dapper;

namespace Test1.Core
{
    /// <summary>
    /// Maps Guid types to and from strings for the Dapper.
    /// </summary>
    public class MySqlGuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public static readonly MySqlGuidTypeHandler Default = new MySqlGuidTypeHandler();

        /// <inheritdoc />
        public override void SetValue(IDbDataParameter parameter, Guid value)
        {
            parameter.Value = value.ToString().ToLower();
        }

        /// <inheritdoc />
        public override Guid Parse(object value)
        {
            switch (value)
            {
                case Guid guid:
                    return guid;

                case string strGuid:
                    return Guid.TryParse(strGuid, out var parsedGuid)
                        ? parsedGuid
                        : Guid.Empty;

                default:
                    return Guid.Empty;
            }
        }
    }
}
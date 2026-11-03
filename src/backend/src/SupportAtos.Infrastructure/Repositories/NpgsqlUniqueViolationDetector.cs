using Microsoft.EntityFrameworkCore;
using Npgsql;
using SupportAtos.Application.Ports;

namespace SupportAtos.Infrastructure.Repositories;

public class NpgsqlUniqueViolationDetector : IUniqueConstraintViolationDetector
{
    public bool IsUniqueViolation(Exception exception)
    {
        for (var atual = exception; atual is not null; atual = atual.InnerException)
        {
            if (atual is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return true;
            }
        }

        return false;
    }
}
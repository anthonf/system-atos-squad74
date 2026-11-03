namespace SupportAtos.Application.Ports;

public interface IUniqueConstraintViolationDetector
{
    bool IsUniqueViolation(Exception exception);
}
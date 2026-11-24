namespace CircuitEditor.Core.Repositories;

/// <summary>Текст лицензии из установленной поставки.</summary>
public interface IInstallationLicenseRepository
{
    /// <summary>Читает LICENSE или возвращает понятное сообщение об ошибке.</summary>
    RepositoryResult<string> Read();
}

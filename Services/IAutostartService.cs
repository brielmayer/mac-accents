namespace MacAccents.Services;

/// <summary>Controls whether the application launches automatically at logon.</summary>
public interface IAutostartService
{
    /// <summary>True if autostart is currently registered for this executable.</summary>
    bool IsEnabled { get; }

    /// <summary>Registers or removes the autostart entry.</summary>
    void SetEnabled(bool enabled);
}

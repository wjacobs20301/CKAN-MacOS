using System.Collections.Generic;
using System.Linq;

using CKAN.Configuration;

namespace CKAN.GUI.Avalonia
{
    public enum GUIModChangeType
    {
        None    = 0,
        Install = 1,
        Remove  = 2,
        Update  = 3,
        Replace = 4,
    }

    /// <summary>
    /// Represents a planned change to a mod installation.
    /// Avalonia port of GUI/Model/ModChange.cs
    /// </summary>
    public class ModChange
    {
        public CkanModule        Mod        { get; }
        public GUIModChangeType  ChangeType { get; }
        public SelectionReason?  Reason     { get; }

        public ModChange(CkanModule mod, GUIModChangeType changeType, SelectionReason? reason)
        {
            Mod        = mod;
            ChangeType = changeType;
            Reason     = reason ?? new SelectionReason.UserRequested();
        }

        public override bool Equals(object? obj)
            => obj is ModChange ch
               && ch.Mod.Equals(Mod)
               && ch.ChangeType.Equals(ChangeType);

        public override int GetHashCode()
            => (Mod?.GetHashCode() ?? 0) ^ ChangeType.GetHashCode();

        public override string ToString()
            => $"{ChangeType} {Mod}";
    }
}

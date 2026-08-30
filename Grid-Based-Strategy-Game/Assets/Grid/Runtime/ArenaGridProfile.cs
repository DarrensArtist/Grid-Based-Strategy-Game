using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace GridBasedStrategyGame.Grid
{
    [Serializable]
    public struct ArenaCellDefinition
    {
        [SerializeField] private bool isActive;
        [SerializeField] private ArenaCellZone zone;

        public bool IsActive => isActive;
        public ArenaCellZone Zone => zone;

        public ArenaCellDefinition(bool isActive, ArenaCellZone zone)
        {
            this.isActive = isActive;
            this.zone = zone;
        }
    }

    [CreateAssetMenu(fileName = "ArenaGridProfile", menuName = "Grid/Arena Grid Profile")]
    public sealed class ArenaGridProfile : ScriptableObject
    {
        public const int CurrentSchemaVersion = 1;

        [Header("Identity")]
        [Tooltip("Stable identity used by runtime state and saved references.")]
        [SerializeField] private string profileId;
        [SerializeField] private string profileDisplayName;
        [TextArea] [SerializeField] private string description;
        [Min(1)] [SerializeField] private int schemaVersion = CurrentSchemaVersion;

        [Header("Geometry")]
        [Min(1)] [SerializeField] private int width = 1;
        [Min(1)] [SerializeField] private int height = 1;
        [Min(float.Epsilon)] [SerializeField] private float cellSize = 1f;

        [Header("Layout")]
        [Tooltip("Row-major definitions: index = z * width + x. Use the Arena Layout Editor when available.")]
        [SerializeField] private ArenaCellDefinition[] cellDefinitions = Array.Empty<ArenaCellDefinition>();

        [Header("Authoring")]
        [TextArea(3, 8)] [SerializeField] private string designerNotes;

        public string ProfileId => profileId;
        public string ProfileDisplayName => profileDisplayName;
        public string Description => description;
        public int SchemaVersion => schemaVersion;
        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public int CellDefinitionCount => cellDefinitions?.Length ?? 0;
        public int ExpectedActiveCellCount => CalculateActiveCellCount();
        public string LayoutChecksum => CalculateLayoutChecksum();
        public string DesignerNotes => designerNotes;

        public bool TryGetCellDefinition(GridCoordinate coordinate, out ArenaCellDefinition definition)
        {
            if (cellDefinitions == null || coordinate.X < 0 || coordinate.X >= width ||
                coordinate.Z < 0 || coordinate.Z >= height)
            {
                definition = default;
                return false;
            }

            var index = (coordinate.Z * width) + coordinate.X;
            if (index < 0 || index >= cellDefinitions.Length)
            {
                definition = default;
                return false;
            }

            definition = cellDefinitions[index];
            return true;
        }

        public int CalculateActiveCellCount()
        {
            if (cellDefinitions == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var definition in cellDefinitions)
            {
                if (definition.IsActive)
                {
                    count++;
                }
            }

            return count;
        }

        public string CalculateLayoutChecksum()
        {
            var canonical = new StringBuilder();
            canonical.Append(schemaVersion).Append('|')
                .Append(width).Append('|')
                .Append(height).Append('|')
                .Append(cellSize.ToString("R", CultureInfo.InvariantCulture)).Append('|');

            if (cellDefinitions == null)
            {
                canonical.Append("null");
            }
            else
            {
                canonical.Append(cellDefinitions.Length).Append('|');
                foreach (var definition in cellDefinitions)
                {
                    canonical.Append(definition.IsActive ? '1' : '0')
                        .Append(':')
                        .Append((int)definition.Zone)
                        .Append(';');
                }
            }

            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes)
                {
                    result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                }

                return result.ToString();
            }
        }

        /// <summary>Creates a non-persistent profile for tests and temporary integration harnesses.</summary>
        public static ArenaGridProfile CreateTransient(
            string profileId,
            int schemaVersion,
            int width,
            int height,
            float cellSize,
            ArenaCellDefinition[] definitions,
            string layoutChecksum,
            int expectedActiveCellCount = -1)
        {
            var profile = CreateInstance<ArenaGridProfile>();
            profile.profileId = profileId;
            profile.profileDisplayName = profileId;
            profile.schemaVersion = schemaVersion;
            profile.width = width;
            profile.height = height;
            profile.cellSize = cellSize;
            profile.cellDefinitions = definitions == null
                ? null
                : (ArenaCellDefinition[])definitions.Clone();
            return profile;
        }
    }
}

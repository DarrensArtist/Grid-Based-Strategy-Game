using UnityEngine;

namespace GridBasedStrategyGame.Grid
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class RuntimeGridHost : MonoBehaviour
    {
        [SerializeField] private ArenaGridProfile profile;
        [SerializeField] private bool initialiseOnAwake = true;

        public RuntimeGrid Grid { get; } = new RuntimeGrid();
        public ArenaGridProfile Profile => profile;

        private void Awake()
        {
            if (initialiseOnAwake)
            {
                Initialise();
            }
        }

        public GridInitializationResult Initialise() => Grid.Initialize(profile, transform);

        public GridInitializationResult Reload() => Grid.Reload(profile, transform);

        public void SetProfile(ArenaGridProfile value) => profile = value;
    }
}

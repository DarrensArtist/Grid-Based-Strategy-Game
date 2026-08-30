using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace GridBasedStrategyGame.Grid.Tests
{
    public sealed class GridModuleSceneTests
    {
        private const string ScenePath = "Assets/Scenes/GridModule.unity";

        [Test]
        public void GridModuleScene_HasReadyToRunDevelopmentPresentationSetup()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var gridRoot = scene.GetRootGameObjects().Single(gameObject => gameObject.name == "Grid Root");
                var host = gridRoot.GetComponent<RuntimeGridHost>();
                var presenter = gridRoot.GetComponent<BattlefieldPresenter>();
                var diagnostics = gridRoot.GetComponent<RuntimeGridDiagnostics>();

                Assert.That(host, Is.Not.Null);
                Assert.That(host.Profile, Is.Not.Null);
                Assert.That(host.Profile.ProfileId, Is.EqualTo("development-arena-5x5"));
                Assert.That(presenter, Is.Not.Null);
                Assert.That(diagnostics, Is.Not.Null);
                Assert.That(diagnostics.AnyLayerVisible, Is.False);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}

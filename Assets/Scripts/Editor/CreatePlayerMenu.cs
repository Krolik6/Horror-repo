using Horror.Player;
using UnityEditor;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// GameObject > Horror > First Person Player builds a ready-to-play player rig in the open scene.
    /// </summary>
    public static class CreatePlayerMenu
    {
        [MenuItem("GameObject/Horror/First Person Player", false, 10)]
        private static void CreatePlayer(MenuCommand command)
        {
            var player = new GameObject("Player");
            Undo.RegisterCreatedObjectUndo(player, "Create First Person Player");

            if (SceneView.lastActiveSceneView != null)
                player.transform.position = SceneView.lastActiveSceneView.pivot;

            var controller = player.AddComponent<CharacterController>();
            controller.radius = 0.3f;
            controller.height = 1.8f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.03f;
            controller.slopeLimit = 45f;

            player.AddComponent<PlayerInputReader>();
            player.AddComponent<PlayerStamina>();
            player.AddComponent<PlayerMovement>();
            player.AddComponent<PlayerDebugHUD>();

            var cameraRoot = new GameObject("CameraRoot");
            cameraRoot.transform.SetParent(player.transform, false);
            cameraRoot.transform.localPosition = new Vector3(0f, 1.68f, 0f);

            // Reuse the scene's main camera if there is one, otherwise make a new one.
            Camera cam = Camera.main;
            if (cam != null)
            {
                Undo.SetTransformParent(cam.transform, cameraRoot.transform, "Move Main Camera");
            }
            else
            {
                var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camGo.tag = "MainCamera";
                camGo.transform.SetParent(cameraRoot.transform, false);
                cam = camGo.GetComponent<Camera>();
            }
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
            cam.nearClipPlane = 0.05f; // stops walls clipping into view when prone or hugging them
            cam.fieldOfView = 70f;

            cameraRoot.AddComponent<PlayerCamera>();

            GameObjectUtility.SetParentAndAlign(player, command.context as GameObject);
            Selection.activeGameObject = player;
        }
    }
}

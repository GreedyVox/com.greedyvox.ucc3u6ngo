using System.Collections;
using Opsive.Shared.Events;
using Opsive.UltimateCharacterController.Camera;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Traits;
using Unity.Netcode;
using UnityEngine;

namespace GreedyVox.NetCode.Tests
{
    public class PlayerManager : NetworkBehaviour
    {
        [SerializeField] private KeyCode m_KeyCodeRespawn = KeyCode.Backspace;
        [SerializeField] private KeyCode m_KeyCodeModelSwitchNext = KeyCode.RightArrow;
        [SerializeField] private KeyCode m_KeyCodeModelSwitchPrevious = KeyCode.LeftArrow;
        private int _PlayerModelCount;
        public int PlayerModelCount =>
        _PlayerModelCount != 0 ? _PlayerModelCount : (_PlayerModelCount = PlayerModel == null ? 1 : PlayerModel.AvailableModels.Length);
        private CharacterRespawner _PlayerRespawner;
        public CharacterRespawner PlayerRespawner =>
        _PlayerRespawner ??= PlayerObject?.GetComponent<CharacterRespawner>();
        private ModelManager _PlayerModel;
        public ModelManager PlayerModel =>
        _PlayerModel ??= PlayerObject?.GetComponent<ModelManager>();
        private NetworkObject _PlayerObject;
        public NetworkObject PlayerObject =>
        _PlayerObject ??= NetworkManager.LocalClient.PlayerObject;
        private int m_ModelIndex;
        public override void OnNetworkSpawn()
        {
            if (!NetworkManager.IsClient) return;
            StartCoroutine(InitializePlayer());
            base.OnNetworkSpawn();
        }
        private void SwitchModels(int idx)
        {
            PlayerModel.ActiveModel = PlayerModel?.AvailableModels[GetModelIndex(idx)];
            if (Camera.main.gameObject?.TryGetComponent(out CameraController cam) ?? false)
                EventHandler.ExecuteEvent<ILookSource>(PlayerModel?.gameObject, "OnCharacterAttachLookSource", cam);
        }
        private int GetModelIndex(int idx) => Mathf.Abs(idx % PlayerModelCount);
        public void RespawnPlayer()
        {
            PlayerRespawner?.Respawn();
            Debug.LogFormat("<color=green>Player Spawned: [<color=white><b>{0}</b></color>]</color>",
            PlayerRespawner?.transform.position);
        }
        private IEnumerator InitializePlayer()
        {
            while (PlayerObject == null)
                yield return null;
            RespawnPlayer();
            StartCoroutine(UpdatePlayer());
        }
        private IEnumerator UpdatePlayer()
        {
            while (isActiveAndEnabled)
            {
                yield return null;
                if (Input.GetKeyUp(m_KeyCodeRespawn))
                    RespawnPlayer();
                else if (Input.GetKeyUp(m_KeyCodeModelSwitchNext))
                    SwitchModels(++m_ModelIndex);
                else if (Input.GetKeyUp(m_KeyCodeModelSwitchPrevious))
                    SwitchModels(--m_ModelIndex);
            }
        }
    }
}
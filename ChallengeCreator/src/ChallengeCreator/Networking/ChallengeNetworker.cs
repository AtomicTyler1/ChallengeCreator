using Photon.Pun;
using ChallengeCreator;
using UnityEngine;
using System.Collections;

namespace ChallengeCreator.Networking
{
    public class ChallengeNetworker : MonoBehaviourPunCallbacks
    {
        public static ChallengeNetworker Instance;
        public bool SuccessfullySynced { get; private set; } = false;

        private void Awake()
        {
            Instance = this;
            if (GetComponent<PhotonView>() == null) gameObject.AddComponent<PhotonView>();
        }

        public void RequestConfigFromHost()
        {
            if (PhotonNetwork.IsMasterClient) return;

            SuccessfullySynced = false;
            Plugin.Log.LogInfo("Requesting challenge from Host...");
            photonView.RPC(nameof(RPC_RequestSync), RpcTarget.MasterClient);

            StartCoroutine(SyncTimeoutRoutine(5f));
        }

        private IEnumerator SyncTimeoutRoutine(float seconds)
        {
            yield return new WaitForSeconds(seconds);

            if (!SuccessfullySynced && !PhotonNetwork.IsMasterClient)
            {
                Plugin.Log.LogError("Failed to get config: Sync Timeout.");
                UIUtils.ChallengeBreakingMessage("Failed to get config, does the host have the mod?", true);
            }
        }

        [PunRPC]
        public void RPC_RequestSync(PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            string json = ChallengeReader.GetCurrentChallengeJson();
            photonView.RPC(nameof(RPC_ReceiveSync), info.Sender, json);
        }

        [PunRPC]
        public void RPC_ReceiveSync(string json)
        {
            SuccessfullySynced = true;
            ChallengeReader.LoadChallengeFromJson(json);

            if (GUIManager.instance != null)
            {
                UIUtils.DisplayChallenge(GUIManager.instance);
            }
        }
    }
}
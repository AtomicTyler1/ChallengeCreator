using Photon.Pun;
using UnityEngine;

namespace ChallengeCreator.Networking
{
    public class Netcode : MonoBehaviourPun
    {
        private static Netcode _instance = null!;
        private PhotonView _photonView = null!;

        public static Netcode Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject singleton = new GameObject("ChallengeCreatorNetcode");
                    _instance = singleton.AddComponent<Netcode>();
                    DontDestroyOnLoad(singleton);
                }
                return _instance;
            }
        }

        public static void EnsureInitialized()
        {
            _ = Instance;
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            _photonView = GetComponent<PhotonView>();
            if (_photonView == null)
            {
                _photonView = gameObject.AddComponent<PhotonView>();
                _photonView.ViewID = 8438;
            }

            DontDestroyOnLoad(gameObject);
        }
    }
}
using UnityEngine;
using BubblesGame.Data.Services;

namespace BubblesGame {
    // Encapsula instância e inicia/parada da gravação das poses
    public class PoseDataManager : MonoBehaviour {
        public static PoseDataManager Instance { get; private set; }
        public PoseDataRecorder PoseRecorder { get; private set; }

        void Awake() {
            if (Instance == null) {
                Instance = this;
                PoseRecorder = gameObject.AddComponent<PoseDataRecorder>();
                DontDestroyOnLoad(gameObject);
            } else {
                Destroy(gameObject);
            }
        }
    }
}

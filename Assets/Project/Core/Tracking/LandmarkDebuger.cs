using UnityEngine;
using Core.MediaPipe;
using Core.Tracking;

public class LandmarkDebugger : MonoBehaviour
{
    public PoseController controller;

    void OnDrawGizmos()
    {
        if (controller == null || controller.SmoothedResult == null) return;

        Gizmos.color = Color.green;

        foreach (var kvp in controller.SmoothedResult.landmarks)
        {
            var lm = kvp.Value;
            Vector3 pos = new Vector3(
                (lm.x - 0.5f) * 2f,
                (lm.y - 0.5f) * -2f,
                lm.z
            );

            Gizmos.DrawSphere(pos, 0.02f);
        }
    }
}

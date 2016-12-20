using UnityEngine;
using System.Collections;

[System.Serializable]
public class MoveQueueItem {
    public Vector3 targetPos;
    public float timeToMove;

    public MoveQueueItem(Vector3 targetPos, float timeToMove) {
        this.targetPos = targetPos;
        this.timeToMove = timeToMove;
    }
}

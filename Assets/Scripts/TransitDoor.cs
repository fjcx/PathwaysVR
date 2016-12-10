using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRStandardAssets.Utils;

public class TransitDoor : MonoBehaviour {

    [SerializeField] private VRInteractiveItem m_InteractiveItem;
    public GameController gameController;

    private void OnEnable() {
        m_InteractiveItem.OnOver += HandleOver;
        m_InteractiveItem.OnOut += HandleOut;
    }


    private void OnDisable() {
        m_InteractiveItem.OnOver -= HandleOver;
        m_InteractiveItem.OnOut -= HandleOut;
    }

    private void HandleOver() {
        Debug.Log("Over Door: ");
        //gameController.FillSelectionBar(3.0f);
        gameController.PlayBlinkEffect();
    }

    private void HandleOut() {
        //gameController.CancelSelectionBar();
        gameController.CancelBlinkTransit();
    }
}

using UnityEngine;
public enum BloomAction { Screen, Previous, Next, Confirm, Pay, Pickup, BoothDoor, Photo, Print }
public class BloomInteractable : MonoBehaviour { public BloomAction action; public int index; }

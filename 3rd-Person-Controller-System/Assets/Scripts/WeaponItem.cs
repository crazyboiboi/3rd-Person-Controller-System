using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Item/Weapon")]
public class WeaponItem : Item
{
    public GameObject modelPrefab;
    public Material glowMaterial;

    public bool isEquipped;
    public bool onLeft;
}

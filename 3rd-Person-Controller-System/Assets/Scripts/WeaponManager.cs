using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Previous weapon = weapon
public class WeaponManager : MonoBehaviour
{
    public WeaponItem[] weaponItems;

    public Transform leftHandSlot;
    public Transform rightHandSlot;

    public Material glowMaterial;

    public GameObject currentWeaponObject;

    [SerializeField]
    bool weaponOut = false;
    float timer = 0f;
    int prevIndex = -1;

    void Update()
    {
        HandleWeaponSelection();
        HandleWeaponReset();
    }

    void HandleWeaponSelection()
    {
        int index = -1;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            index = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            index = 1;

        if (index != -1)
        {
            weaponOut = true;
            timer = 0f;
            LoadWeapon(index);
        }
    }

    void HandleWeaponReset()
    {
        if (!weaponOut)
            return;

        if (CheckForAction())
        {
            timer = 0f;
        }
        else
        {
            timer += Time.deltaTime;
            if(timer > 2.0f)
            {
                weaponOut = false;
                prevIndex = -1;
                StartCoroutine(UnequipWeapon(0.5f));
            }
        }
    }

    bool CheckForAction()
    {
        return false;
    }

    void LoadWeapon(int index)
    {
        if(currentWeaponObject != null)
        {
            if(index != prevIndex)
            {
                prevIndex = index;
                StartCoroutine(UnequipWeapon(0.5f));
                LoadWeaponOnHand(weaponItems[index], weaponItems[index].onLeft);
            } 
        } else
        {
            prevIndex = index;
            LoadWeaponOnHand(weaponItems[index], weaponItems[index].onLeft);
        }
    }

    public void LoadWeaponOnHand(WeaponItem weaponItem, bool isLeft)
    {
        //Assign the appropriate slot based on isLeft bool
        Transform slot = isLeft ? leftHandSlot : rightHandSlot;
        
        //Equip weapon and play weapon animation
        StartCoroutine(EquipWeapon(weaponItem.modelPrefab, 0.2f, slot));
    }

    void LoadWeaponModel(GameObject weaponModel, Transform handSlot)
    {
        GameObject weapon = Instantiate(weaponModel);

        //Set up the weapon on the hand slot assigned 
        if (weapon != null)
        {
            weapon.transform.parent = handSlot;

            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            weapon.transform.localScale = Vector3.one;
        }
        currentWeaponObject = weapon;
    }

    public IEnumerator EquipWeapon(GameObject weaponModel, float duration, Transform handSlot)
    {
        LoadWeaponModel(weaponModel, handSlot);
        currentWeaponObject.SetActive(false);
        GameObject weaponClone = Instantiate(currentWeaponObject, currentWeaponObject.transform.position, currentWeaponObject.transform.rotation);

        yield return new WaitForSeconds(0.2f);

        //Apply glow material to weapon clone
        MeshRenderer cloneMR = weaponClone.GetComponentInChildren<MeshRenderer>();
        Material[] materials = cloneMR.materials;

        for (int i = 0; i < materials.Length; i++)
        {
            Material m = glowMaterial;
            materials[i] = m;
        }

        cloneMR.materials = materials;
        weaponClone.SetActive(true);

        //Apply tweening for weapon clone's glow (can use DOTween in the future)
        float time = 0;
        while (time < duration)
        {
            for (int i = 0; i < cloneMR.materials.Length; i++)
                cloneMR.materials[i].SetFloat("_AlphaThreshold", Mathf.Lerp(1f, 0f, time / duration));

            time += Time.deltaTime;
            yield return null;
        }

        Destroy(weaponClone);
        currentWeaponObject.SetActive(true);
    }
    
    public IEnumerator UnequipWeapon(float duration)
    {
        GameObject weaponClone = Instantiate(currentWeaponObject, currentWeaponObject.transform.position, currentWeaponObject.transform.rotation);
        Destroy(currentWeaponObject.gameObject);
        
        //Do I need to set to null?
        currentWeaponObject = null;

        yield return new WaitForSeconds(0.2f);

        //Apply glow material to weapon clone
        MeshRenderer cloneMR = weaponClone.GetComponentInChildren<MeshRenderer>();
        Material[] materials = cloneMR.materials;

        for (int i = 0; i < materials.Length; i++)
        {
            Material m = glowMaterial;
            materials[i] = m;
        }

        cloneMR.materials = materials;
        weaponClone.SetActive(true);

        //Apply tweening for weapon clone's glow (can use DOTween in the future)
        float time = 0;
        while (time < duration)
        {
            for (int i = 0; i < cloneMR.materials.Length; i++)
                cloneMR.materials[i].SetFloat("_AlphaThreshold", Mathf.Lerp(0f, 1f, time / duration));

            time += Time.deltaTime;
            yield return null;
        }

        Destroy(weaponClone);
    }
}

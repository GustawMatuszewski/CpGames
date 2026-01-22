using UnityEngine;
using System.Collections.Generic;


public class Construction : MonoBehaviour
{
    [Header("References")]
    public List<GameObject> connectors;

    [Header("Settings")]
    public GameObject Model;
    public List<Item> itemsList;
    public float timeToBuild;
    public bool canBeBurnt;
    public float tempHealth;

}
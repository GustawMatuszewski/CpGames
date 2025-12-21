using UnityEngine;
using System.Collections.Generic;
public class Construction : MonoBehaviour
{
    [Header("References")]
    public List<Connector> connectors;

    [Header("Settings")]
    public GameObject Model;
    public List<Item> itemsList;
    public float timeToBuild;
    public bool canBeBurnt;
    public float tempHealth; //Change with new EntityStatus
}

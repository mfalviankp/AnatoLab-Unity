using UnityEngine;

public class OrganData : MonoBehaviour
{
    public string organName = "Nama Organ";
    [TextArea(3, 5)]
    public string description = "Deskripsi penjelasan fungsi organ di sini.";
}
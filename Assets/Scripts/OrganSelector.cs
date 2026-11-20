using UnityEngine;
using TMPro; // Menggunakan TextMeshPro untuk UI Text

public class OrganSelector : MonoBehaviour
{
    [Header("UI References")]
    public GameObject infoPanel;
    public TMP_Text titleText;
    public TMP_Text descriptionText;

    void Start()
    {
        // Sembunyikan panel UI saat pertama kali Play
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }
    }

    void Update()
    {
        // Deteksi klik kiri mouse
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Tembakkan Raycast dari kamera ke arah kursor
            if (Physics.Raycast(ray, out hit))
            {
                // Cek apakah objek yang diklik memiliki komponen OrganData
                OrganData organ = hit.collider.GetComponent<OrganData>();

                if (organ != null)
                {
                    // Tampilkan data organ ke UI
                    ShowInfo(organ.organName, organ.description);
                }
            }
        }
    }

    void ShowInfo(string title, string desc)
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(true);
            if (titleText != null) titleText.text = title;
            if (descriptionText != null) descriptionText.text = desc;
        }
    }

    // Fungsi untuk menutup pop-up via tombol Close
    public void ClosePanel()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }
    }
}
using UnityEngine;

public class PlatformRescueZone : MonoBehaviour
{
    [Header("Configuració de la Missió")]
    [SerializeField] private string rescueMissionID = "salvar_animals"; // ID de la teva missió de rescat

    [Header("Referències de l'Animal")]
    [SerializeField] private GameObject trappedAnimal; // L'objecte de l'animal que està atrapat

    private bool isBridgeConnected = false;

    private void OnTriggerEnter(Collider other)
    {
        // Si el pont ja s'ha creat, no cal fer res més
        if (isBridgeConnected) return;

        // DETECCIÓ: Si el que creua el buit és un pont de teranyina (capa Climbing)
        if (other.gameObject.layer == LayerMask.NameToLayer("Climbing"))
        {
            isBridgeConnected = true;
            Debug.Log($"[RESCAT] S'ha creat un pont de teranyina vàlid a la zona: {name}!");

            // 1. Sumem +1 al comptador de la missió global
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.ProgressMission(rescueMissionID, 1);
            }

            // 2. Canviem el diàleg de l'animal automàticament perquè ens doni les gràcies
            if (trappedAnimal != null)
            {
                MissionNPC npcScript = trappedAnimal.GetComponent<MissionNPC>();
                if (npcScript != null)
                {
                    npcScript.dialegInicial = "Uau! Gràcies per fer aquest pont de teranyina, ja puc creuar sa i estalvi!";
                    npcScript.dialegEnProgres = "Gràcies per fer aquest pont de teranyina, ja puc creuar sa i estalvi!";
                }

                // OPCIONAL: Si vols que l'animal celebre o passi alguna cosa visual, ho pots fer aquí
                // ex: trappedAnimal.transform.localScale *= 1.1f; (creix una mica de l'alegria)
            }
        }
    }
}

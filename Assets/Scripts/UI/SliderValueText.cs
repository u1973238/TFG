using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Mostra el valor d'un control lliscant al seu costat, per exemple «80 %» o «1,5×»
public class SliderValueText : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private bool asPercentage = true; // Cert: valors de 0 a 1 en percentatge. Fals: multiplicador (1,0×)

    private float shownValue = float.NaN;

    void Update()
    {
        // Es comprova a cada fotograma perquè, en obrir el menú, el valor canvia sense avisar (SetValueWithoutNotify)
        if (slider != null && slider.value != shownValue) Refresh();
    }

    public void Refresh()
    {
        if (slider == null || text == null) return;

        shownValue = slider.value;
        // En català el símbol de percentatge va separat amb un espai (que no es pot partir) i els decimals amb coma
        text.text = asPercentage
            ? Mathf.RoundToInt(shownValue * 100f) + " %"
            : shownValue.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',') + "×";
    }
}

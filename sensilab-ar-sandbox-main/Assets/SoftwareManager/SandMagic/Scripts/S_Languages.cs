using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class S_Languages : MonoBehaviour
{

    private bool active = false;
    public TMP_Dropdown languageDropdown;

    public void ChangeLocale(int localeID)
    {
        Debug.Log("ChangeLocale called with localeID: " + localeID);

        if (!active)
        {
            StartCoroutine(SetLocale(localeID));
        }
    }

    IEnumerator SetLocale(int _localeID)
    {
        active = true;
        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[_localeID];
        active = false;
    }
    
    void Start()
    {
        // Configura o Dropdown com os nomes dos idiomas disponíveis.
        languageDropdown.ClearOptions();
        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            languageDropdown.options.Add(new TMP_Dropdown.OptionData(locale.name));
        }

        // Adiciona um ouvinte para chamar ChangeLocale quando o valor do Dropdown é alterado.
        languageDropdown.onValueChanged.AddListener(ChangeLocale);
    }

    // Certifique-se de remover o ouvinte quando o script for destruído.
    private void OnDestroy()
    {
        languageDropdown.onValueChanged.RemoveListener(ChangeLocale);
    }
}
